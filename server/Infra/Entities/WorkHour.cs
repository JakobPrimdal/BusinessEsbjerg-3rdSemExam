using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("work_hour")]
public class WorkHour
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column,NotNull]
    public string UserId { get; set; } = "";

    [Column,NotNull]
    public DateOnly ValidFrom { get; set; }

    [Column,NotNull]
    public decimal MonHours { get; set; }

    [Column,NotNull]
    public decimal TueHours { get; set; }

    [Column,NotNull]
    public decimal WedHours { get; set; }

    [Column,NotNull]
    public decimal ThuHours { get; set; }

    [Column,NotNull]
    public decimal FriHours { get; set; }

    [Column,NotNull]
    public decimal SatHours { get; set; }

    [Column,NotNull]
    public decimal SunHours { get; set; }

    [Column,NotNull]
    public DateTime CreatedAt { get; set; }
}

