using LinqToDB;
using LinqToDB.Data;

namespace Infrastructure.Seed;


public static class SchemaUpgrade
{
    public static void Run(HolidayDb db)
    {
        AddColumnIfMissing(db, "holiday_transaction", "holiday_request_id", sameTypeAs: "holiday_account_id");

        // tables that are no longer used
        db.Execute("DROP TABLE IF EXISTS salary");
        db.Execute("DROP TABLE IF EXISTS rule_value");
    }

    private static void AddColumnIfMissing(DataConnection db, string table, string column, string sameTypeAs)
    {
        var columns = db.Query<TableColumn>($"SELECT name AS Name, type AS Type FROM pragma_table_info('{table}')")
            .ToList();

        if (columns.Any(c => c.Name == column))
            return;

        var type = columns.First(c => c.Name == sameTypeAs).Type;
        db.Execute($"ALTER TABLE {table} ADD COLUMN {column} {type} NULL");
    }

    private sealed class TableColumn
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }
}