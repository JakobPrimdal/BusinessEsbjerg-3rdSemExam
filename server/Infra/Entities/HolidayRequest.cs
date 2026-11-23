using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_request")]
public class HolidayRequest
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column,NotNull]
    public string UserId { get; set; } 

    // Which balance it spends from: HolidayService.Weeks1To5 or HolidayService.Week6
    [Column,NotNull]
    public string HolidayType { get; set; } 

    [Column,NotNull]
    public DateOnly StartDate { get; set; }

    // The last day of the holiday (inclusive)
    [Column,NotNull]
    public DateOnly EndDate { get; set; }
    
    [Column,NotNull]
    public decimal Hours { get; set; }

    // See HolidayRequestStatus
    [Column,NotNull]
    public string Status { get; set; } = HolidayRequestStatus.Pending;
    
    [Column]
    public string? DecidedBy { get; set; }

    [Column]
    public DateTime? DecidedAt { get; set; }

    // Required when a request is denied
    [Column]
    public string? DecisionReason { get; set; }

    [Column,NotNull]
    public DateTime CreatedAt { get; set; }
}


public static class HolidayRequestStatus
{
    public const string Pending = "pending";     // waiting for the accountant ("Afventer")
    public const string Approved = "approved";   // approved by the accountant
    public const string Denied = "denied";       // denied by the accountant with a reason
    public const string Cancelled = "cancelled"; // cancelled 

    // Requests that count as planned holiday (they block overlapping requests and use balance)
    public static readonly string[] Active = [Pending, Approved];
}
