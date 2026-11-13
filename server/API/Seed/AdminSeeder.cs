using System.Security.Cryptography;
using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.Security;

namespace Api.Seed;

public class AdminSeeder(
    LoginDb db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration)
{
    public void Seed()
    {
        var username = configuration["Admin:Username"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin credentials are missing from configuration.");
        }

        // Check if admin already exists
        if (db.Users.Any(u => u.Username == username))
            return;

        // Generate a unique salt for hashing
        var salt = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(16));

        // Hash password
        var passwordHash = passwordHasher.HashPassword(
            password, salt);

        // Insert admin
        db.InsertAsync(new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            PasswordHash = passwordHash,
            Role = UserRole.Admin,
            IsActive = true
        });
    }
}