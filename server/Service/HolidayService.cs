using System.Globalization;
using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;

public class HolidayService(HolidayDb db, TimeProvider clock)
{
    public const string EarnedType = "earned";

    public const string Weeks1To5 = "weeks_1_5";
    public const string Week6 = "week_6";

    public const string DaysPerMonthKey = "weeks_1_5_days_per_month";
    public const string Week6HoursPerMonthKey = "week_6_hours_per_month";

    private const decimal FullTimeWeeklyHours = 37m;

    public int AddMonthlyHolidayForAll(DateOnly anyDayInMonth)
    {
        var month = FirstDayOfMonth(anyDayInMonth);
        var lastDay = month.AddMonths(1).AddDays(-1);

        var userIds = db.WorkHours
            .Where(w => w.ValidFrom <= lastDay)
            .Select(w => w.UserId)
            .Distinct()
            .ToList();

        var added = 0;
        foreach (var userId in userIds)
        {
            if (AddMonthlyHoliday(userId, month))
                added++;
        }
        return added;
    }

    public bool AddMonthlyHoliday(string userId, DateOnly anyDayInMonth)
    {
        var month = FirstDayOfMonth(anyDayInMonth);

        var earning = CalculateMonth(userId, month);
        if (earning is null)
            return false; // did not work at all this month (not started yet, or stopped)

        var addedWeeks1To5 = AddEarned(userId, month, Weeks1To5, earning.WeeklyHours,
            earning.Weeks1To5Rule, earning.Weeks1To5Hours);

        var addedWeek6 = false;
        if (earning.Week6Rule != null)
        {
            addedWeek6 = AddEarned(userId, month, Week6, earning.WeeklyHours,
                earning.Week6Rule, earning.Week6Hours);
        }

        return addedWeeks1To5 || addedWeek6;
    }

    public HolidayBalanceDTO GetBalance(string userId)
    {
        EnsureUserExists(userId);
        var accounts = db.HolidayAccounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.HolidayType)
            .ThenByDescending(a => a.EarningStart)
            .ToList();

        var transactions = db.HolidayTransactions
            .Where(t => t.UserId == userId)
            .ToList();

        var accountDtos = accounts.Select(a =>
        {
            var mine = transactions.Where(t => t.HolidayAccountId == a.Id).ToList();
            var months = mine
                .Where(t => t.Type == EarnedType && t.PeriodMonth != null)
                .OrderBy(t => t.PeriodMonth)
                .Select(t => new MonthlyHolidayDTO(t.PeriodMonth!.Value, t.Hours, t.WeeklyHoursUsed))
                .ToList();

            return new HolidayAccountDTO(
                a.Id, a.HolidayType,
                a.EarningStart, a.EarningEnd, a.UsageStart, a.UsageEnd, a.TransferDeadline,
                a.Status,
                BalanceHours: mine.Sum(t => t.Hours),
                Months: months);
        }).ToList();

        decimal Open(string type) =>
            accountDtos.Where(a => a.HolidayType == type && a.Status == "open").Sum(a => a.BalanceHours);

        return new HolidayBalanceDTO(userId, Open(Weeks1To5), Open(Week6), accountDtos);
    }

    public WorkHour AddWorkHours(CreateWorkHourDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId))
            throw new BadRequestException("UserId is required");
        
        EnsureUserExists(dto.UserId);
        
        decimal[] days = [dto.MonHours, dto.TueHours, dto.WedHours, dto.ThuHours, dto.FriHours, dto.SatHours, dto.SunHours];
        if (days.Any(h => h < 0 || h > 24))
            throw new BadRequestException("Hours per day must be between 0 and 24");

        var row = new WorkHour
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            ValidFrom = dto.ValidFrom,
            MonHours = dto.MonHours,
            TueHours = dto.TueHours,
            WedHours = dto.WedHours,
            ThuHours = dto.ThuHours,
            FriHours = dto.FriHours,
            SatHours = dto.SatHours,
            SunHours = dto.SunHours,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        db.Insert(row);
        return row;
    }

    public static decimal CalculateWeeks1To5Hours(decimal weeklyHours, decimal daysPerMonth)
        => Math.Round(daysPerMonth * weeklyHours / 5m, 4, MidpointRounding.AwayFromZero);

    public static decimal CalculateWeek6Hours(decimal weeklyHours, decimal fullTimeHoursPerMonth)
        => Math.Round(fullTimeHoursPerMonth * weeklyHours / FullTimeWeeklyHours, 4, MidpointRounding.AwayFromZero);

    public static decimal WeeklyHours(WorkHour w)
        => w.MonHours + w.TueHours + w.WedHours + w.ThuHours + w.FriHours + w.SatHours + w.SunHours;

    public static DateOnly HolidayYearStart(DateOnly date)
        => new(date.Month >= 9 ? date.Year : date.Year - 1, 9, 1);

    public static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
    public static DateOnly PreviousMonth(DateOnly today) => FirstDayOfMonth(today).AddMonths(-1);

    // One work schedule and how many calendar days of a month it covers
    public record SchedulePeriod(decimal WeeklyHours, int Days);

    // Splits a month into the periods each work schedule covers
    // Days before the first schedule  are not included
    // A schedule with 0 hours gives a period with 0 weekly hours
    public static List<SchedulePeriod> DaysPerSchedule(IReadOnlyList<WorkHour> schedules, DateOnly month)
    {
        var firstDay = FirstDayOfMonth(month);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var periods = new List<SchedulePeriod>();

        for (var i = 0; i < schedules.Count; i++)
        {
            // A schedule is valid from its ValidFrom until the day before the next schedule starts
            var validFrom = schedules[i].ValidFrom;
            var validTo = i + 1 < schedules.Count ? schedules[i + 1].ValidFrom.AddDays(-1) : DateOnly.MaxValue;

            var start = validFrom > firstDay ? validFrom : firstDay;
            var end = validTo < lastDay ? validTo : lastDay;
            if (end < start)
                continue; // this schedule is not valid in this month

            periods.Add(new SchedulePeriod(WeeklyHours(schedules[i]), end.DayNumber - start.DayNumber + 1));
        }
        return periods;
    }

    // The share of a full month's holiday you get for 'daysWorked' out of 'daysInMonth' calendar days
    public static decimal ProRate(decimal fullMonthHours, int daysWorked, int daysInMonth)
        => Math.Round(fullMonthHours * daysWorked / daysInMonth, 4, MidpointRounding.AwayFromZero);
    
    public static HolidayAccount NewAccountFor(string holidayType, string userId, DateOnly month)
    {
        if (holidayType == Week6)
        {
            var y = month.Year;
            return new HolidayAccount
            {
                HolidayType = Week6,
                UserId = userId,
                EarningStart = new DateOnly(y, 1, 1),
                EarningEnd = new DateOnly(y, 12, 31),
                UsageStart = new DateOnly(y + 1, 5, 1),
                UsageEnd = new DateOnly(y + 2, 4, 30),
                TransferDeadline = new DateOnly(y + 2, 9, 30)
            };
        }

        var start = HolidayYearStart(month);
        return new HolidayAccount
        {
            HolidayType = Weeks1To5,
            UserId = userId,
            EarningStart = start,
            EarningEnd = new DateOnly(start.Year + 1, 8, 31),
            UsageStart = start,
            UsageEnd = new DateOnly(start.Year + 1, 12, 31),
            TransferDeadline = new DateOnly(start.Year + 1, 12, 31)
        };
    }

    private bool AddEarned(string userId, DateOnly month, string holidayType,
        decimal weeklyHours, RuleValue rule, decimal hours)
    {
        var account = GetOrCreateAccount(userId, holidayType, month);

        var alreadyAdded = db.HolidayTransactions.Any(t =>
            t.HolidayAccountId == account.Id &&
            t.Type == EarnedType &&
            t.PeriodMonth == month);
        if (alreadyAdded)
            return false;

        var now = clock.GetUtcNow().UtcDateTime;
        db.Insert(new HolidayTransaction
        {
            Id = Guid.NewGuid(),
            HolidayAccountId = account.Id,
            UserId = userId,
            Type = EarnedType,
            Hours = hours,
            TransactionDate = DateOnly.FromDateTime(now),
            PeriodMonth = month,
            WeeklyHoursUsed = weeklyHours,
            RuleValidFrom = rule.ValidFrom,
            Reason = "Monthly holiday",
            CreatedBy = null,
            CreatedAt = now
        });
        return true;
    }

    private HolidayAccount GetOrCreateAccount(string userId, string holidayType, DateOnly month)
    {
        var template = NewAccountFor(holidayType, userId, month);

        var existing = db.HolidayAccounts.FirstOrDefault(a =>
            a.UserId == userId &&
            a.HolidayType == holidayType &&
            a.EarningStart == template.EarningStart);
        if (existing != null)
            return existing;

        template.Id = Guid.NewGuid();
        template.Status = "open";
        template.CreatedAt = clock.GetUtcNow().UtcDateTime;
        db.Insert(template);
        return template;
    }

    // What a user earns in one month, before it is saved
    private record MonthEarning(
        decimal WeeklyHours,
        RuleValue Weeks1To5Rule,
        decimal Weeks1To5Hours,
        RuleValue? Week6Rule,
        decimal Week6Hours);

    // Calculates the holiday for one month. Each work schedule counts for the calendar days it covers,
    // so starting, stopping or changing hours in the middle of a month earns only for those days.
    private MonthEarning? CalculateMonth(string userId, DateOnly month)
    {
        var lastDay = month.AddMonths(1).AddDays(-1);
        var schedules = db.WorkHours
            .Where(w => w.UserId == userId && w.ValidFrom <= lastDay)
            .OrderBy(w => w.ValidFrom)
            .ThenBy(w => w.CreatedAt)
            .ToList();

        var periods = DaysPerSchedule(schedules, month);
        if (periods.Sum(p => p.WeeklyHours * p.Days) == 0)
            return null;

        var weeks1To5Rule = FindRule(DaysPerMonthKey, month)
            ?? throw new InvalidOperationException($"No rule '{DaysPerMonthKey}' for {month}");
        var week6Rule = FindRule(Week6HoursPerMonthKey, month);
        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

        decimal weeks1To5Hours = 0;
        decimal week6Hours = 0;
        foreach (var period in periods)
        {
            weeks1To5Hours += ProRate(CalculateWeeks1To5Hours(period.WeeklyHours, ParseRule(weeks1To5Rule)),
                period.Days, daysInMonth);

            if (week6Rule != null)
                week6Hours += ProRate(CalculateWeek6Hours(period.WeeklyHours, ParseRule(week6Rule)),
                    period.Days, daysInMonth);
        }

        // Weekly hours saved on the transaction: the schedule in force at the end of the month
        return new MonthEarning(WeeklyHours(schedules[^1]), weeks1To5Rule, weeks1To5Hours, week6Rule, week6Hours);
    }

    private RuleValue? FindRule(string key, DateOnly month)
        => db.RuleValues
            .Where(r => r.Key == key && r.ValidFrom <= month)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();

    private static decimal ParseRule(RuleValue rule)
        => decimal.Parse(rule.Value, CultureInfo.InvariantCulture);
    
    private void EnsureUserExists(string userId)
    {
        if (!db.Users.Any(u => u.UserId == userId))
            throw new NotFoundException("User not found");
    }
}