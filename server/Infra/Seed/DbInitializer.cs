using Infrastructure.Entities;
using LinqToDB;
using LinqToDB.Data;

namespace Infrastructure.Seed;

public static class DbInitializer
{
    public static void LoginDbInitialize(LoginDb db)
    {
        db.CreateTable<User>(tableOptions: TableOptions.CreateIfNotExists);
        db.CreateTable<Role>(tableOptions: TableOptions.CreateIfNotExists);

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var roleId = (int)role;
            if (!db.Roles.Any(r => r.Id == roleId))
                db.Insert(new Role { Id = roleId, Name = role.ToString() });
        }
    }

    public static void HolidayRuleSetDbInitialize(HolidayDb db, TimeProvider tp)
    {
        db.CreateTable<HolidayRuleSet>(tableOptions: TableOptions.CreateIfNotExists);
        HolidayRuleSetSeeder.Seed(db, tp);
    }
    public static void HolidayRequestDbInitialize(HolidayDb db)
    {
        db.CreateTable<HolidayRequest>(tableOptions: TableOptions.CreateIfNotExists);

        var existing = db.Query<string>("SELECT name FROM pragma_table_info('HolidayRequest')").ToHashSet();
        (string Name, string Type)[] added =
        [
            (nameof(HolidayRequest.CancellationRequestedAt), "TEXT"),
            (nameof(HolidayRequest.CancellationDecidedBy), "TEXT"),
            (nameof(HolidayRequest.CancellationDecidedAt), "TEXT"),
            (nameof(HolidayRequest.CancellationDecisionReason), "TEXT")
        ];
        foreach (var (name, type) in added)
        {
            if (!existing.Contains(name))
                db.Execute($"ALTER TABLE HolidayRequest ADD COLUMN {name} {type} NULL");
        }
    }
}