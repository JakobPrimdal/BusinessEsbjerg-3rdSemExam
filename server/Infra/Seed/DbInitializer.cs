using Infrastructure.Entities;
using Infrastructure.Entities.Enums;
using LinqToDB;

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

    public static void HolidayDbInitialize(HolidayDb db, TimeProvider tp)
    {
        db.CreateTable<ClosedDay>(tableOptions: TableOptions.CreateIfNotExists);
        ClosedDaySeeder.Seed(db);
    }
}