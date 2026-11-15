using System.Text;
using Infrastructure.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Service.Security;

namespace UnitTests;

public class TestTokenService
{
    private const string Secret = "unit-test-secret-that-is-at-least-32-chars-long";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";

    private static readonly User TestUser = new()
    {
        UserId = "user-123",
        Username = "jakob",
        Role = UserRole.Accountant
    };

    private readonly TokenService _sut = new(new JwtSettings(Secret, Issuer, Audience));

    private TokenValidationParameters ValidationParameters(string secret) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };

    [Fact]
    public void CreateToken_ContainsSubUsernameAndRoleClaims()
    {
        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateToken(TestUser));

        Assert.Equal("user-123", token.GetClaim(JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("jakob", token.GetClaim(JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("Accountant", token.GetClaim("role").Value);
    }

    [Fact]
    public void CreateToken_ExpiresInAboutTwoHours()
    {
        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.CreateToken(TestUser));

        var expected = DateTime.UtcNow.AddHours(2);
        Assert.InRange(token.ValidTo, expected.AddMinutes(-1), expected.AddMinutes(1));
    }

    [Fact]
    public async Task CreateToken_IsOnlyValidWhenCheckedWithTheCorrectSecret()
    {
        var token = _sut.CreateToken(TestUser);
        var handler = new JsonWebTokenHandler();

        // Also checks issuer and audience, since ValidationParameters requires both to match
        var withCorrectSecret = await handler.ValidateTokenAsync(token, ValidationParameters(Secret));
        var withWrongSecret = await handler.ValidateTokenAsync(
            token, ValidationParameters("a-completely-different-secret-of-32-plus-chars"));

        Assert.True(withCorrectSecret.IsValid);
        Assert.False(withWrongSecret.IsValid);
    }
}