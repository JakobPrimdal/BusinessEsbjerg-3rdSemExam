using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class HolidayRequest
{
    [PrimaryKey] public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string HolidayType { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Hours { get; set; }
    public string Status { get; set; } = HolidayRequestStatus.Pending;
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class HolidayRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Denied = "denied";
    public const string Cancelled = "cancelled";

    // pending and approved requests count as planned holiday
    public static readonly string[] Active = [Pending, Approved];
}