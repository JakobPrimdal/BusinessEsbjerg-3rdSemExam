using Infrastructure.Entities;
using Infrastructure.Interfaces;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace Infrastructure;

public class LoginDb(DataOptions<LoginDb> options) : DataConnection(options.Options), ILoginDb
{
    private ITable<User> Users => this.GetTable<User>();

    public ITable<Role> Roles => this.GetTable<Role>();

    public Task<bool> UsernameExistsAsync(string username)
    {
        return Users.AnyAsync(u => u.Username == username);
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        return Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        return Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task AddUserAsync(User user)
    {
        await this.InsertAsync(user);
    }

    public Task<bool> RoleExistsAsync(UserRole role)
    {
        return Users.AnyAsync(u => u.Role == role);
    }
}