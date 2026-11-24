using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class HolidayAccount
{
    [PrimaryKey] public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string HolidayType { get; set; } = "";
    public DateOnly EarningStart { get; set; }
    public DateOnly EarningEnd { get; set; }
    public DateOnly UsageStart { get; set; }
    public DateOnly UsageEnd { get; set; }
    public DateOnly TransferDeadline { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
}