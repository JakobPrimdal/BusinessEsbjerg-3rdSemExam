using Infrastructure.Entities;
using Infrastructure.Entities.Enums;
using Infrastructure.Interfaces;
using NSubstitute;
using Service;
using Service.DTOs;
using Service.Exceptions;
using Service.Interfaces;

namespace UnitTests;

public class TestAuthService
{
    private readonly ILoginDb _db = Substitute.For<ILoginDb>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly AuthService _sut;

    public TestAuthService()
    {
        _sut = new AuthService(_db, _tokenService, _hasher);
    }

    private static User ActiveUser() => new()
    {
        UserId = "user-1",
        Username = "alice",
        PasswordHash = "stored-hash",
        Role = UserRole.User,
        IsActive = true
    };

    // Register tests

    [Fact]
    public async Task Register_WithValidRequest_SavesUserWithHashedPasswordAndReturnsDto()
    {
        _hasher.HashAndSaltPassword("password123").Returns("hashed-pw");

        var result = await _sut.Register(new RegisterRequestDTO("  alice  ", "password123", "Accountant"));

        await _db.Received(1).AddUserAsync(Arg.Is<User>(u =>
            u.Username == "alice" &&              // trimmed
            u.PasswordHash == "hashed-pw" &&      // hashed, not plain text
            u.Role == UserRole.Accountant &&
            u.IsActive));

        Assert.Equal("alice", result.Username);
        Assert.Equal(UserRole.Accountant, result.Role.Id);
        Assert.True(result.IsActive);
        Assert.False(string.IsNullOrEmpty(result.UserId));
    }

    public static TheoryData<string, string> InvalidCredentials => new()
    {
        { "ab", "password123" },                  // username 1 below minimum
        { "   ", "password123" },                 // whitespace only -> empty after trim
        { new string('a', 31), "password123" },   // username 1 above maximum
        { "alice", "123456" },                    // password 1 below minimum
    };

    [Theory]
    [MemberData(nameof(InvalidCredentials))]
    public async Task Register_WithInvalidUsernameOrPassword_ThrowsBadRequestAndSavesNothing(
        string username, string password)
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.Register(new RegisterRequestDTO(username, password, "User")));

        await _db.DidNotReceive().AddUserAsync(Arg.Any<User>());
    }

    public static TheoryData<string, string> BoundaryCredentials => new()
    {
        { "abc", "1234567" },                     // exactly the minimum lengths
        { new string('a', 30), "1234567" },       // exactly the maximum username length
    };

    [Theory]
    [MemberData(nameof(BoundaryCredentials))]
    public async Task Register_WithCredentialsAtLengthBoundaries_Succeeds(string username, string password)
    {
        var result = await _sut.Register(new RegisterRequestDTO(username, password, "User"));

        Assert.Equal(username, result.Username);
        await _db.Received(1).AddUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyTaken_ThrowsBadRequestAndSavesNothing()
    {
        _db.UsernameExistsAsync(Arg.Any<string>()).Returns(true);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.Register(new RegisterRequestDTO("alice", "password123", "User")));

        Assert.Equal("Username is already taken", ex.Message);
        await _db.DidNotReceive().AddUserAsync(Arg.Any<User>());
    }

    [Theory]
    [InlineData("admin", UserRole.Admin)]
    [InlineData("ACCOUNTANT", UserRole.Accountant)]
    [InlineData("User", UserRole.User)]
    public async Task Register_ParsesRoleCaseInsensitively(string role, UserRole expected)
    {
        var result = await _sut.Register(new RegisterRequestDTO("alice", "password123", role));
        
        Assert.Equal(expected, result.Role.Id);
    }

    [Theory]
    [InlineData("SuperUser")]  // not a role
    [InlineData("99")]         // numeric but undefined (Enum.TryParse alone would accept this)
    [InlineData("")]
    [InlineData(null)]
    public async Task Register_WithUnsupportedRole_ThrowsBadRequestAndSavesNothing(string? role)
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.Register(new RegisterRequestDTO("alice", "password123", role!)));

        await _db.DidNotReceive().AddUserAsync(Arg.Any<User>());
    }

    // Login tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var user = ActiveUser();
        _db.GetUserByUsernameAsync("alice").Returns(user);
        _hasher.VerifyHashedPassword("password123", "stored-hash").Returns(true);
        _tokenService.CreateToken(user).Returns("jwt-token");

        var result = await _sut.Login(new LoginRequestDTO("  alice  ", "password123"));

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("user-1", result.User.UserId);
        Assert.Equal("alice", result.User.Username);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ThrowsUnauthorized()
    {
        _db.GetUserByUsernameAsync(Arg.Any<string>()).Returns((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.Login(new LoginRequestDTO("ghost", "password123")));

        Assert.Equal("Invalid username or password", ex.Message);
        _tokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedWithSameMessageAsUnknownUser()
    {
        _db.GetUserByUsernameAsync("alice").Returns(ActiveUser());
        _hasher.VerifyHashedPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.Login(new LoginRequestDTO("alice", "wrong-password")));

        // Same message as the unknown-user case, so attackers can't tell which usernames exist
        Assert.Equal("Invalid username or password", ex.Message);
        _tokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsUnauthorizedEvenWithCorrectPassword()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _db.GetUserByUsernameAsync("alice").Returns(user);
        _hasher.VerifyHashedPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.Login(new LoginRequestDTO("alice", "password123")));

        _tokenService.DidNotReceive().CreateToken(Arg.Any<User>());
    }

    // GetUser tests

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsDto()
    {
        _db.GetUserByIdAsync("user-1").Returns(ActiveUser());

        var result = await _sut.GetUser("user-1");

        Assert.Equal("user-1", result.UserId);
        Assert.Equal("alice", result.Username);
        Assert.Equal(UserRole.User, result.Role.Id);
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ThrowsNotFound()
    {
        _db.GetUserByIdAsync("missing").Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUser("missing"));
    }
}