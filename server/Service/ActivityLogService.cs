using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;
using Service.Interfaces;

namespace Service;

// Uses the same scoped HolidayDb as the holiday services. A log written during a request
// therefore goes through the same connection, and is part of any transaction that is open,
// so a rolled-back change never leaves a log row behind.
public class ActivityLogService(HolidayDb db, TimeProvider clock) : IActivityLogService
{
    public const int PageSize = 50;
    public const string SystemUsername = "System";
    public const string InvalidDateRangeError = "Fra-datoen skal ligge før til-datoen";

    public void Log(string? userId, string action, string details)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        var username = userId is null
            ? SystemUsername
            : db.Users.Where(u => u.UserId == userId).Select(u => u.Username).FirstOrDefault() ?? userId;

        db.Insert(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Action = action,
            Details = details,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        });
    }

    public ActivityLogPageDTO GetLogs(string? userId, DateTimeOffset? from, DateTimeOffset? to, int page)
    {
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
            throw new BadRequestException(InvalidDateRangeError);
        if (page < 1)
            page = 1;

        var query = db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(l => l.UserId == userId);

        if (from.HasValue)
        {
            var fromUtc = from.Value.UtcDateTime;
            query = query.Where(l => l.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.UtcDateTime;
            query = query.Where(l => l.CreatedAt < toUtc);
        }

        var total = query.Count();
        var items = query
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id) // stable order between pages when two rows have the same time
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return new ActivityLogPageDTO(items.Select(ToDTO).ToList(), total, page, PageSize);
    }

    public List<ActivityLogUserDTO> GetUsers()
        => db.Users
            .OrderBy(u => u.Username)
            .Select(u => new ActivityLogUserDTO(u.UserId, u.Username))
            .ToList();

    // SQLite gives the time back without a kind. Mark it as UTC, so the JSON gets a "Z"
    // and the browser converts it to Danish time correctly.
    private static ActivityLogDTO ToDTO(ActivityLog l)
        => new(l.Id, l.UserId, l.Username, l.Action, l.Details, DateTime.SpecifyKind(l.CreatedAt, DateTimeKind.Utc));
}