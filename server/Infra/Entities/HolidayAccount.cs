using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_account")]
public class HolidayAccount
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    [NotNull]
    public string UserId { get; set; } = "";

    [Column("holiday_type")]
    [NotNull]
    public string HolidayType { get; set; } = "";

    [Column("earning_start")]
    [NotNull]
    public DateOnly EarningStart { get; set; }

    [Column("earning_end")]
    [NotNull]
    public DateOnly EarningEnd { get; set; }

    [Column("usage_start")]
    [NotNull]
    public DateOnly UsageStart { get; set; }

    [Column("usage_end")]
    [NotNull]
    public DateOnly UsageEnd { get; set; }

    [Column("transfer_deadline")]
    [NotNull]
    public DateOnly TransferDeadline { get; set; }

    [Column("status")]
    [NotNull]
    public string Status { get; set; } = "open";

    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; }
}