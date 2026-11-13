using Infrastructure.Entities;

namespace Service.DTOs;

public class UserDTO
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}