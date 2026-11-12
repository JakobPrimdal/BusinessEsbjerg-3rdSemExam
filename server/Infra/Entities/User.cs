using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class User
{
    [PrimaryKey] public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}