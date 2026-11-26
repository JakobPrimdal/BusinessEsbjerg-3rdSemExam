using Infrastructure.Entities;
using Infrastructure.Entities.Enums;

namespace Infrastructure.Interfaces;

public interface ILoginDb
{
    public Task<bool> UsernameExistsAsync(string username);
    public Task<User?> GetUserByUsernameAsync(string username);
    public Task<User?> GetUserByIdAsync(string userId);
    public Task AddUserAsync(User user);
    public Task<bool> RoleExistsAsync(UserRole role);
}