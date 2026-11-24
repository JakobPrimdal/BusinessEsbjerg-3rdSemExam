using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class HolidayTransaction
{
    [PrimaryKey] public Guid Id { get; set; }
    public Guid HolidayAccountId { get; set; }
    public string UserId { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Hours { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly? PeriodMonth { get; set; }
    public decimal? WeeklyHoursUsed { get; set; }
    public DateOnly? RuleValidFrom { get; set; }
    public Guid? HolidayRequestId { get; set; }
    public string? Reason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}