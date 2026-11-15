using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Service.Interfaces;

namespace Api.Seed;

public class AdminSeeder(
    ILoginDb db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        var username = configuration["Admin:Username"]?.Trim().ToLowerInvariant();
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin credentials are missing from configuration.");
        }

        if (await db.UsernameExistsAsync(username))
            return;

        await db.AddUserAsync(new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            PasswordHash = passwordHasher.HashAndSaltPassword(password),
            Role = UserRole.Admin,
            IsActive = true
        });
    }
}