using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("holiday_account")]
public class HolidayAccount
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column,NotNull]
    public string UserId { get; set; } 

    [Column,NotNull]
    public string HolidayType { get; set; } 
    [Column,NotNull]
    public DateOnly EarningStart { get; set; }

    [Column,NotNull]
    public DateOnly EarningEnd { get; set; }

    [Column,NotNull]
    public DateOnly UsageStart { get; set; }

    [Column,NotNull]
    public DateOnly UsageEnd { get; set; }

    [Column,NotNull]
    public DateOnly TransferDeadline { get; set; }

    [Column,NotNull]
    public string Status { get; set; } 

    [Column ,NotNull]
    public DateTime CreatedAt { get; set; }
}