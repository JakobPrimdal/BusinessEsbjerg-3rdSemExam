using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_transaction")]
public class HolidayTransaction
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("holiday_account_id")]
    [NotNull]
    public Guid HolidayAccountId { get; set; }

    [Column("user_id")]
    [NotNull]
    public Guid UserId { get; set; }

    [Column("type")]
    [NotNull]
    public string Type { get; set; } = "";

    [Column("hours")]
    [NotNull]
    public decimal Hours { get; set; }

    [Column("transaction_date")]
    [NotNull]
    public DateOnly TransactionDate { get; set; }

    [Column("period_month")]
    public DateOnly? PeriodMonth { get; set; }

    [Column("qualifying_salary")]
    public decimal? QualifyingSalary { get; set; }

    [Column("weekly_hours_used")]
    public decimal? WeeklyHoursUsed { get; set; }

    [Column("rule_valid_from")]
    public DateOnly? RuleValidFrom { get; set; }

    [Column("reverses_transaction_id")]
    public Guid? ReversesTransactionId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; }
}