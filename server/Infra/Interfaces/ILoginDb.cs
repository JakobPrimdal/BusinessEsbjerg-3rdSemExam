using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface ILoginDb
{
    public Task<bool> UsernameExistsAsync(string username);
    public Task<User?> GetUserByUsernameAsync(string username);
    public Task<User?> GetUserByIdAsync(string userId);
    public Task AddUserAsync(User user);
}