using Infrastructure.Entities;
using Infrastructure.Entities.Enums;
using LinqToDB;

namespace Infrastructure.Seed;

public static class ClosedDaySeeder
{
    private const int FirstYear = 2025;
    private const int LastYear = 2036;
    public static void Seed(HolidayDb db)
    {
        // Do not overwrite existing public holiday data
        if (db.ClosedDays.Any(cd => cd.Type == ClosedDayType.PublicHoliday))
            return;
        
        var now = DateTime.UtcNow;
        var toInsert = new List<ClosedDay>();

        // Fixed-date holidays: one recurring row each, starting at FirstYear.
        AddFixed(1, 1, "Nytårsdag");
        AddFixed(12, 25, "Juledag");
        AddFixed(12, 26, "2. juledag");

        // Moving holidays: one non-recurring row per year, based on Easter.
        for (var year = FirstYear; year <= LastYear; year++)
        {
            var easter = CalculateEasterSunday(year);

            AddMoving(easter.AddDays(-3), "Skærtorsdag");
            AddMoving(easter.AddDays(-2), "Langfredag");
            AddMoving(easter, "Påskedag");
            AddMoving(easter.AddDays(1), "2. påskedag");
            AddMoving(easter.AddDays(39), "Kristi himmelfartsdag");
            AddMoving(easter.AddDays(49), "Pinsedag");
            AddMoving(easter.AddDays(50), "2. pinsedag");
        }

        foreach (var closedDay in toInsert)
            db.Insert(closedDay);

        void AddFixed(int month, int day, string name)
        {
            var date = new DateOnly(FirstYear, month, day);

            toInsert.Add(Create(name, date, isRecurring: true));
        }

        void AddMoving(DateOnly date, string name)
        {
            toInsert.Add(Create(name, date, isRecurring: false));
        }

        ClosedDay Create(string name, DateOnly date, bool isRecurring) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = ClosedDayType.PublicHoliday,
            Date = date,
            IsRecurring = isRecurring,
            RecurringUntil = null,
            CreatedAt = now,
            UpdatedAt = null,
            IsDeleted = false
        };
    }

    // Anonymous Gregorian algorithm (Meeus/Jones/Butcher).
    private static DateOnly CalculateEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;

        return new DateOnly(year, month, day);
    }
}