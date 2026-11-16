namespace Service.DTOs;

public class UserDTO
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public RoleDTO Role { get; set; } = new();
    public bool IsActive { get; set; } = true;
}