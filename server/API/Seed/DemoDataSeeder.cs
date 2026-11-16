using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Service;
using Service.DTOs;
using Service.Interfaces;

namespace Api.Seed;

// Development only: a ready-made employee with real holiday data, so the frontend has something to show.
// Login: employee / password
public class DemoDataSeeder(
    ILoginDb loginDb,
    IPasswordHasher passwordHasher,
    HolidayService holidayService,
    TimeProvider clock,
    ILogger<DemoDataSeeder> logger)
{
    private const string Username = "employee";
    private const string Password = "password";
    private static readonly DateOnly EmployedFrom = new(2025, 9, 1);

    public async Task SeedAsync()
    {
        var user = await loginDb.GetUserByUsernameAsync(Username);
        if (user is null)
        {
            user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = Username,
                PasswordHash = passwordHasher.HashAndSaltPassword(Password),
                Role = UserRole.User,
                IsActive = true
            };
            await loginDb.AddUserAsync(user);

            // Full time, 37 h a week: Mon-Thu 7.5 h, Fri 7 h
            holidayService.AddWorkHours(new CreateWorkHourDTO(
                user.UserId, EmployedFrom, 7.5m, 7.5m, 7.5m, 7.5m, 7m, 0m, 0m));

            logger.LogInformation("Demo employee '{Username}' created", Username);
        }

        // Earn holiday for every finished month since employment, through the same
        // service the monthly job uses. Safe to repeat: months already earned are skipped.
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var lastMonth = HolidayService.PreviousMonth(today);
        var added = 0;
        for (var month = EmployedFrom; month <= lastMonth; month = month.AddMonths(1))
        {
            if (holidayService.AddMonthlyHoliday(user.UserId, month))
                added++;
        }

        if (added > 0)
            logger.LogInformation("Demo employee '{Username}': holiday earned for {Count} month(s)", Username, added);
    }
}
