using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("salary")]
public class Salary
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

    [Column("pay_type")]
    [NotNull]
    public string PayType { get; set; } = "salaried";

    [Column("monthly_salary")]
    [NotNull]
    public decimal MonthlySalary { get; set; }

    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; }
}

