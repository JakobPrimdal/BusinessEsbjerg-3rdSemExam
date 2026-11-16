using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_request")]
public class HolidayRequest
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    [NotNull]
    public string UserId { get; set; } = "";

    // Which balance it spends from: HolidayService.Weeks1To5 or HolidayService.Week6
    [Column("holiday_type")]
    [NotNull]
    public string HolidayType { get; set; } = "";

    [Column("start_date")]
    [NotNull]
    public DateOnly StartDate { get; set; }

    // The last day of the holiday (inclusive)
    [Column("end_date")]
    [NotNull]
    public DateOnly EndDate { get; set; }
    
    [Column("hours")]
    [NotNull]
    public decimal Hours { get; set; }

    // See HolidayRequestStatus
    [Column("status")]
    [NotNull]
    public string Status { get; set; } = HolidayRequestStatus.Pending;
    
    [Column("decided_by")]
    public string? DecidedBy { get; set; }

    [Column("decided_at")]
    public DateTime? DecidedAt { get; set; }

    // Required when a request is denied
    [Column("decision_reason")]
    public string? DecisionReason { get; set; }

    [Column("created_at")]
    [NotNull]
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
