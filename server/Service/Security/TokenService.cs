using System.Security.Claims;
using System.Text;
using Infrastructure.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Service.Security;
public record JwtSettings(string Secret, string Issuer, string Audience);

public class TokenService(JwtSettings settings)
{
    public string CreateToken(User user)
    {
        // Turn Secret into a key object the signing algorithm can use(check appsettings/Development.json).
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            // claims : the facts stored inside the token
            Subject = new ClaimsIdentity([
                
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),              
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                
                new Claim("role", user.Role.ToString())                                     
            ]),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            
            Expires = DateTime.UtcNow.AddHours(2),
            
            // Sign the token with the Secret 
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
    }
}