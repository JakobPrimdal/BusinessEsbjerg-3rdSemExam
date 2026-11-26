using Infrastructure.Entities.Enums;

namespace IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Service.DTOs;
using Xunit;

// WebApplicationFactory<Program> starts our real API in memory (real routing, auth, cookies, SQLite).
// xUnit creates it once for this class and shares it between the tests.
public class TestAuthController(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin-password-123";
    private const string UserPassword = "password123";

    // The jwt cookie is marked Secure, and a client only sends a Secure cookie back over https.
    private static readonly WebApplicationFactoryClientOptions HttpsClient = new()
    {
        BaseAddress = new Uri("https://localhost")
    };

    // Only one accountant may exist, and the database is shared by all tests in this class,
    // so the accountant is created once and reused.
    private static readonly SemaphoreSlim AccountantLock = new(1, 1);
    private static string? _accountantUsername;

    static TestAuthController()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "integration-tests.db");
        File.Delete(dbPath); // test from an empty database file on every run

        Environment.SetEnvironmentVariable("ConnectionStrings__Db", $"Data Source={dbPath};Foreign Keys=True");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "integration-test-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "integration-test-audience");
        Environment.SetEnvironmentVariable("Admin__Username", AdminUsername);
        Environment.SetEnvironmentVariable("Admin__Password", AdminPassword);
    }

    // A new test class instance is created per test, so every test gets a fresh client
    private readonly HttpClient _client = factory.CreateClient(HttpsClient);

    // helpers

    private static async Task LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/Login", new LoginRequestDTO(username, password));
        response.EnsureSuccessStatusCode(); // the jwt cookie is now stored in this client
    }

    private Task LoginAsAdminAsync() =>
        LoginAsync(_client, AdminUsername, AdminPassword);

    // Registers a new user through a separate admin client and returns the username
    private async Task<string> CreateUserAsync(string role = "User")
    {
        var username = "user" + Guid.NewGuid().ToString("N")[..10];
        var adminClient = factory.CreateClient(HttpsClient);
        await LoginAsync(adminClient, AdminUsername, AdminPassword);

        var response = await adminClient.PostAsJsonAsync("/Register", new RegisterRequestDTO(username, UserPassword, role));
        response.EnsureSuccessStatusCode();

        return username;
    }

    // Creates the one allowed accountant on first use and returns the same username afterwards
    private async Task<string> EnsureAccountantAsync()
    {
        await AccountantLock.WaitAsync();
        try
        {
            return _accountantUsername ??= await CreateUserAsync("Accountant");
        }
        finally
        {
            AccountantLock.Release();
        }
    }

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("detail").GetString();
    }

    // POST /Login tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUserAndSetsHardenedJwtCookie()
    {
        // Arrange
        var request = new LoginRequestDTO(AdminUsername, AdminPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/Login", request);
        var user = await response.Content.ReadFromJsonAsync<UserDTO>();

        // Assert: correct user returned
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AdminUsername, user!.Username);
        Assert.Equal(UserRole.Admin, user.Role.Id);

        // Assert: the token travels in an HttpOnly + Secure + SameSite=Strict cookie, not in the body
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var jwtCookie = Assert.Single(cookies, c => c.StartsWith("jwt="));
        Assert.StartsWith("jwt=ey", jwtCookie);
        Assert.Contains("httponly", jwtCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", jwtCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", jwtCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AdminUsername, "wrong-password")]
    [InlineData("unknown-username", "password123")]
    public async Task Login_WithBadCredentials_Returns401WithoutCookie(string username, string password)
    {
        // Arrange
        var request = new LoginRequestDTO(username, password);

        // Act
        var response = await _client.PostAsJsonAsync("/Login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid username or password", await ReadProblemDetailAsync(response));
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    // POST /Register tests

    [Fact]
    public async Task Register_AsAdmin_CreatesUserWhoCanThenLogIn()
    {
        // Arrange: a normal "User" role, because Admin and Accountant can only exist once
        await LoginAsAdminAsync();
        var username = "user" + Guid.NewGuid().ToString("N")[..10];
        var request = new RegisterRequestDTO(username, UserPassword, "User");

        // Act
        var response = await _client.PostAsJsonAsync("/Register", request);
        var created = await response.Content.ReadFromJsonAsync<UserDTO>();

        // Assert: user was created
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(username, created!.Username);
        Assert.Equal(UserRole.User, created.Role.Id);
        Assert.True(created.IsActive);

        // Act & Assert: the new user can log in - proves it was saved and the password was hashed correctly
        var newUserClient = factory.CreateClient(HttpsClient);
        await LoginAsync(newUserClient, username, UserPassword);
        var me = await newUserClient.GetFromJsonAsync<UserDTO>("/GetMe");
        Assert.Equal(created.UserId, me!.UserId);
    }

    [Fact]
    public async Task Register_WithTooShortPassword_Returns400WithMessage()
    {
        // Arrange
        await LoginAsAdminAsync();
        var request = new RegisterRequestDTO("validname" + Guid.NewGuid().ToString("N")[..6], "123456", "User");

        // Act
        var response = await _client.PostAsJsonAsync("/Register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Password must be at least 7 characters", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Register_WithAlreadyTakenUsername_Returns400()
    {
        // Arrange
        var existingUsername = await CreateUserAsync();
        await LoginAsAdminAsync();
        var request = new RegisterRequestDTO(existingUsername, UserPassword, "User");

        // Act
        var response = await _client.PostAsJsonAsync("/Register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Username is already taken", await ReadProblemDetailAsync(response));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Accountant")]
    public async Task Register_WhenRoleAlreadyExists_Returns400(string role)
    {
        // Arrange: make sure an accountant exists (the admin is seeded at startup)
        await EnsureAccountantAsync();
        await LoginAsAdminAsync();
        var request = new RegisterRequestDTO("user" + Guid.NewGuid().ToString("N")[..10], UserPassword, role);

        // Act
        var response = await _client.PostAsJsonAsync("/Register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("It is not possible to create more", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Register_WhenRoleAlreadyExists_DoesNotCreateTheUser()
    {
        // Arrange
        await EnsureAccountantAsync();
        await LoginAsAdminAsync();
        var username = "user" + Guid.NewGuid().ToString("N")[..10];

        // Act
        await _client.PostAsJsonAsync("/Register", new RegisterRequestDTO(username, UserPassword, "Admin"));

        // Assert: the rejected user must not be able to log in
        var response = await factory.CreateClient(HttpsClient)
            .PostAsJsonAsync("/Login", new LoginRequestDTO(username, UserPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // GET /GetMe tests

    [Fact]
    public async Task GetMe_WhenLoggedIn_ReturnsTheCurrentUser()
    {
        // Arrange
        var username = await EnsureAccountantAsync();
        await LoginAsync(_client, username, UserPassword);

        // Act
        var response = await _client.GetAsync("/GetMe");
        var me = await response.Content.ReadFromJsonAsync<UserDTO>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(username, me!.Username);
        Assert.Equal(UserRole.Accountant, me.Role.Id);
    }

    // POST /Logout tests

    [Fact]
    public async Task Logout_ExpiresJwtCookieAndEndsTheSession()
    {
        // Arrange
        var username = await CreateUserAsync();
        await LoginAsync(_client, username, UserPassword);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/GetMe")).StatusCode); // sanity check: logged in

        // Act
        var response = await _client.PostAsync("/Logout", content: null);

        // Assert: server tells the browser to delete the cookie
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var jwtCookie = Assert.Single(cookies, c => c.StartsWith("jwt="));
        Assert.Contains("expires=Thu, 01 Jan 1970", jwtCookie, StringComparison.OrdinalIgnoreCase);

        // Assert: the client dropped the cookie, so we are logged out now
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/GetMe")).StatusCode);
    }

    // GET /AdminCheck tests

    [Fact]
    public async Task AdminCheck_AsAdmin_Returns200()
    {
        // Arrange
        await LoginAsAdminAsync();

        // Act
        var response = await _client.GetAsync("/AdminCheck");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("You are an admin", await response.Content.ReadAsStringAsync());
    }

    // Authorization - all protected endpoints

    [Theory]
    [InlineData("GET", "/GetMe")]
    [InlineData("GET", "/AdminCheck")]
    [InlineData("POST", "/Register")]
    [InlineData("GET", "/activitylog/GetActivityLog")]
    [InlineData("GET", "/activitylog/GetUsers")]
    public async Task ProtectedEndpoints_WithoutLogin_Return401(string method, string path)
    {
        // Arrange: _client has not logged in
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new RegisterRequestDTO("whoever", "password123", "User"));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/AdminCheck")]
    [InlineData("POST", "/Register")]
    public async Task AdminOnlyEndpoints_AsNonAdmin_Return403(string method, string path)
    {
        // Arrange: logged in, but not as an Admin
        var username = await EnsureAccountantAsync();
        await LoginAsync(_client, username, UserPassword);
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new RegisterRequestDTO("whoever", "password123", "User"));

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    // Activity log

    [Fact]
    public async Task ActivityLog_AsAdmin_Returns200WithAPage()
    {
        // Arrange
        await LoginAsAdminAsync();

        // Act
        var response = await _client.GetAsync("/activitylog/GetActivityLog?page=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<ActivityLogPageDTO>();
        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
    }

    // OpenAPI tests

    [Fact]
    public async Task OpenApiDocument_ReturnsSuccessAndValidJson()
    {
        // Act: verify the OpenAPI endpoint contract
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}