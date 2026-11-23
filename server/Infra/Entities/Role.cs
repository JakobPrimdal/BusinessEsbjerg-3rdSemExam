using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("Role")]
public class Role
{
    [PrimaryKey, Column] public int Id { get; set; }
    
    [Column, NotNull]
    public string Name { get; set; } = "";
}