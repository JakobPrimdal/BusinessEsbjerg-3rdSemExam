using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Service;
using Service.DTOs;

namespace Api.Controllers;

public class AuthController(AuthService authService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost(nameof(Register))]
    public async Task<UserDTO> Register([FromBody] RegisterRequestDTO dto)
    {
        return await authService.Register(dto);
    }

    [HttpPost(nameof(Login))]
    public async Task<UserDTO> Login([FromBody] LoginRequestDTO dto)
    {
        var result =  await authService.Login(dto);
        
        // The token goes into an HttpOnly cookie, not into the JSON body
        Response.Cookies.Append("jwt", result.Token, CookieSettings(DateTimeOffset.UtcNow.AddHours(2)));

        return result.User;
    }
    
    [HttpPost(nameof(Logout))]
    public IActionResult Logout()
    {
        // JavaScript can't remove an HttpOnly cookie, so the server has to
        Response.Cookies.Delete("jwt", CookieSettings(null));
        return NoContent();
    }
    
    [Authorize]
    [HttpGet(nameof(GetMe))]
    public async Task<UserDTO> GetMe()
    {
        var userId = HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        return await authService.GetUser(userId);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet(nameof(AdminCheck))]
    public string AdminCheck()
    {
      return "You are an admin";  
    }
    
    private static CookieOptions CookieSettings(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expires
    };
}