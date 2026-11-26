using Infrastructure.Entities.Enums;
using LinqToDB.Mapping;

namespace Infrastructure.Entities;

public class ClosedDay
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Column, NotNull]
    public string Name { get; set; } = "";

    [Column, NotNull]
    public ClosedDayType Type { get; set; }
    
    [Column, NotNull]
    public DateOnly Date { get; set; }
    
    [Column, NotNull]
    public bool IsRecurring { get; set; }
    
    [Column]
    public DateOnly? RecurringUntil { get; set; }

    [Column, NotNull]
    public DateTime CreatedAt { get; set; }

    [Column]
    public DateTime? UpdatedAt { get; set; }
    
    [Column, NotNull]
    public bool IsDeleted { get; set; }
}