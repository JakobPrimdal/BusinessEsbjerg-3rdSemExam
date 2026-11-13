using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("work_hour")]
public class WorkHour
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    [NotNull]
    public string UserId { get; set; } = "";

    [Column("valid_from")]
    [NotNull]
    public DateOnly ValidFrom { get; set; }

    [Column("mon_hours")]
    [NotNull]
    public decimal MonHours { get; set; }

    [Column("tue_hours")]
    [NotNull]
    public decimal TueHours { get; set; }

    [Column("wed_hours")]
    [NotNull]
    public decimal WedHours { get; set; }

    [Column("thu_hours")]
    [NotNull]
    public decimal ThuHours { get; set; }

    [Column("fri_hours")]
    [NotNull]
    public decimal FriHours { get; set; }

    [Column("sat_hours")]
    [NotNull]
    public decimal SatHours { get; set; }

    [Column("sun_hours")]
    [NotNull]
    public decimal SunHours { get; set; }

    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; }
}

