using Infrastructure.Entities.Enums;

namespace Service.DTOs;

public class RoleDTO
{
    public UserRole Id { get; set; }
    public string Name { get; set; } = "";
}