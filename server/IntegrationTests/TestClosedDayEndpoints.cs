using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure.Entities.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.DTOs;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IntegrationTests;

public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user"), new Claim(ClaimTypes.Role, role.ToString())], SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class ApiFactory : WebApplicationFactory<Program>
{
    // Own DB file, so these tests never shares (or re-seeds) the one TestAuthController uses.
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), "closedday-test.db");

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Db", $"Data Source={_dbPath};Foreign Keys=True");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "integration-test-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "integration-test-audience");
        Environment.SetEnvironmentVariable("Admin__Username", "admin");
        Environment.SetEnvironmentVariable("Admin__Password", "Admin-password-123");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Switch out real authentication for the header-based fake.
        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;
        try { File.Delete(_dbPath); } catch (IOException) { /* temp file, not important */ }
    }
}

public class ClosedDayEndpointTests : IClassFixture<ApiFactory>
{
    private const string Route = "holiday/";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _anonymous;
    private readonly HttpClient _user;
    private readonly HttpClient _admin;

    private readonly ClosedDayRequestDTO _valid = new()
    {
        Name = "Firma udflugt",
        Type = ClosedDayType.CompanyHoliday,
        Date = new DateOnly(2100, 1, 1).AddDays(Random.Shared.Next(0, 30000)),
        IsRecurring = false,
        RecurringUntil = null
    };

    public ClosedDayEndpointTests(ApiFactory factory)
    {
        _anonymous = factory.CreateClient();

        _user = factory.CreateClient();
        _user.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "User");

        _admin = factory.CreateClient();
        _admin.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsOk_AndRowAppearsInGetClosedDays()
    {
        var response = await _admin.PostAsJsonAsync(Route + "CreateClosedDay", _valid);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClosedDayResponseDTO>(Json);
        Assert.NotNull(created);

        var all = await _user.GetFromJsonAsync<List<ClosedDayResponseDTO>>(Route + "GetClosedDays", Json);
        Assert.Contains(all!, x => x.Id == created.Id && x.Name == _valid.Name);
    }

    [Fact]
    public async Task ToggleDelete_AsAdmin_MarksRowDeleted_AndStillReturnsItInGetClosedDays()
    {
        var created = await (await _admin.PostAsJsonAsync(Route + "CreateClosedDay", _valid))
            .Content.ReadFromJsonAsync<ClosedDayResponseDTO>(Json);

        var response = await _admin.PostAsync($"{Route}ToggleDelete?id={created!.Id}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var all = await _user.GetFromJsonAsync<List<ClosedDayResponseDTO>>(Route + "GetClosedDays", Json);
        Assert.True(all!.Single(x => x.Id == created.Id).IsDeleted);
    }

    [Fact]
    public async Task Create_AsAnonymous_Returns401()
    {
        var response = await _anonymous.PostAsJsonAsync(Route + "CreateClosedDay", _valid);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsNonAdmin_Returns403()
    {
        var response = await _user.PostAsJsonAsync(Route + "CreateClosedDay", _valid);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidRequest_Returns400()
    {
        var response = await _admin.PostAsJsonAsync(Route + "CreateClosedDay", _valid with { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await _admin.PostAsJsonAsync($"{Route}UpdateClosedDay?id={Guid.NewGuid()}", _valid);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}