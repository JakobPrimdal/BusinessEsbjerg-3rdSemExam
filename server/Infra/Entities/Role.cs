using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("Role")]
public class Role
{
    [PrimaryKey] public int Id { get; set; }
    public string Name { get; set; } = "";
}