using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_transaction")]
public class HolidayTransaction
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column,NotNull]
    public Guid HolidayAccountId { get; set; }

    [Column,NotNull]
    public string UserId { get; set; } = "";

    [Column,NotNull]
    public string Type { get; set; } = "";

    [Column,NotNull]
    public decimal Hours { get; set; }

    [Column]
    [NotNull]
    public DateOnly TransactionDate { get; set; }

    [Column]
    public DateOnly? PeriodMonth { get; set; }

    [Column]
    public decimal? WeeklyHoursUsed { get; set; }

    [Column]
    public DateOnly? RuleValidFrom { get; set; }
    
    [Column]
    public Guid? HolidayRequestId { get; set; }

    [Column]
    public string? Reason { get; set; }

    [Column]
    public string? CreatedBy { get; set; }

    [Column,NotNull]
    public DateTime CreatedAt { get; set; }
}