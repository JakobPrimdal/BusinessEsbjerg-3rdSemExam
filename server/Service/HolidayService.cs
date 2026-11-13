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
        var lastDay = month.AddMonths(1).AddDays(-1);

        var schedule = db.WorkHours
            .Where(w => w.UserId == userId && w.ValidFrom <= lastDay)
            .OrderByDescending(w => w.ValidFrom)
            .FirstOrDefault();
        if (schedule is null)
            return false;

        var weeklyHours = WeeklyHours(schedule);

        var weeks1To5Rule = FindRule(DaysPerMonthKey, month)
            ?? throw new InvalidOperationException($"No rule '{DaysPerMonthKey}' for {month}");
        var addedWeeks1To5 = AddEarned(userId, month, Weeks1To5, weeklyHours, weeks1To5Rule,
            CalculateWeeks1To5Hours(weeklyHours, ParseRule(weeks1To5Rule)));

        var addedWeek6 = false;
        var week6Rule = FindRule(Week6HoursPerMonthKey, month);
        if (week6Rule != null)
        {
            addedWeek6 = AddEarned(userId, month, Week6, weeklyHours, week6Rule,
                CalculateWeek6Hours(weeklyHours, ParseRule(week6Rule)));
        }

        return addedWeeks1To5 || addedWeek6;
    }

    public HolidayBalanceDTO GetBalance(string userId)
    {
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

    private RuleValue? FindRule(string key, DateOnly month)
        => db.RuleValues
            .Where(r => r.Key == key && r.ValidFrom <= month)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();

    private static decimal ParseRule(RuleValue rule)
        => decimal.Parse(rule.Value, CultureInfo.InvariantCulture);
}