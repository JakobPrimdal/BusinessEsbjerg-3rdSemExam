using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("rule_value")]
public class RuleValue
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("key")]
    [NotNull]
    public string Key { get; set; } = "";

    [Column("value")]
    [NotNull]
    public string Value { get; set; } = "";

    [Column("valid_from")]
    [NotNull]
    public DateOnly ValidFrom { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; }
}