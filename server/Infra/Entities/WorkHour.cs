using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class WorkHour
{
    [PrimaryKey] public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateOnly ValidFrom { get; set; }
    public decimal MonHours { get; set; }
    public decimal TueHours { get; set; }
    public decimal WedHours { get; set; }
    public decimal ThuHours { get; set; }
    public decimal FriHours { get; set; }
    public decimal SatHours { get; set; }
    public decimal SunHours { get; set; }
    public DateTime CreatedAt { get; set; }
}