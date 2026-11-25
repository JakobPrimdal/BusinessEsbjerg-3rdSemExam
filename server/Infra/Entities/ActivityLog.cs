using LinqToDB.Mapping;

namespace Infrastructure.Entities;

// One row per action someone performed. Rows are only ever inserted, never updated or deleted.
public class ActivityLog
{
    [PrimaryKey] public Guid Id { get; set; }

    // Who did it. Null when the system did it (e.g. the monthly holiday job).
    public string? UserId { get; set; }

    // Copy of the username at the time of the action, so the log still reads correctly
    // if the user is renamed or deactivated later.
    public string Username { get; set; } = "";

    // What kind of action, one of ActivityLogActions
    public string Action { get; set; } = "";

    // Human-readable description in Danish, e.g. "Godkendte ferie for anna: 01.12.2026–05.12.2026"
    public string Details { get; set; } = "";

    // When, in UTC
    public DateTime CreatedAt { get; set; }
}

// The action codes the frontend translates to Danish labels.
// Add a constant here (and a label in the frontend) when a new action starts being logged.
public static class ActivityLogActions
{
    public const string UserCreated = "user.created";
    public const string WorkHoursAdded = "workhours.added";
    public const string MonthlyHolidayAdded = "holiday.monthly-added";
    public const string HolidayRequested = "holiday.requested";
    public const string HolidayApproved = "holiday.approved";
    public const string HolidayDenied = "holiday.denied";
    public const string RuleSetCreated = "ruleset.created";
    public const string RuleSetToggled = "ruleset.toggled";
}