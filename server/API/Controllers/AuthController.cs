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
    public async Task<LoginResponseDTO> Login([FromBody] LoginRequestDTO dto)
        => await authService.Login(dto);
    
    [Authorize]  //means only with a valid token
    [HttpGet(nameof(GetMe))]
    public async Task<UserDTO> GetMe()
    {
        var userId = HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        return await authService.GetUser(userId);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet(nameof(AdminCheck))]
    public string AdminCheck() => "You are an admin";
}