using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using LinqToDB;
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
    HolidayDb holidayDb,
    IConfiguration configuration,
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

        await SeedActivityLogAsync(user);
    }

    // Example log rows spread over the last weeks, so the activity log page and its filters
    // can be tried before the real actions write logs. Only added while the log is empty.
    private async Task SeedActivityLogAsync(User employee)
    {
        if (holidayDb.ActivityLogs.Any())
            return;

        var adminName = configuration["Admin:Username"]?.Trim() ?? "";
        var admin = await loginDb.GetUserByUsernameAsync(adminName);
        var now = clock.GetUtcNow().UtcDateTime;

        void Add(User? who, int daysAgo, string action, string details) => holidayDb.Insert(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = who?.UserId,
            Username = who?.Username ?? ActivityLogService.SystemUsername,
            Action = action,
            Details = details,
            CreatedAt = now.AddDays(-daysAgo)
        });

        Add(admin, 20, ActivityLogActions.UserCreated, $"Oprettede brugeren {employee.Username} (Medarbejder)");
        Add(admin, 20, ActivityLogActions.WorkHoursAdded, $"Registrerede arbejdstid for {employee.Username}: 37 timer/uge fra 01.09.2025");
        Add(null, 14, ActivityLogActions.MonthlyHolidayAdded, "Månedlig ferie optjent for 1 bruger");
        Add(employee, 6, ActivityLogActions.HolidayRequested, "Anmodede om ferie 22.12.2026–31.12.2026 (uge 1–5)");
        Add(admin, 3, ActivityLogActions.RuleSetCreated, "Oprettede regelsæt gældende fra 01.09.2027");
        Add(admin, 1, ActivityLogActions.RuleSetToggled, "Deaktiverede regelsæt gældende fra 01.09.2027");

        logger.LogInformation("Demo activity log rows added");
    }
}