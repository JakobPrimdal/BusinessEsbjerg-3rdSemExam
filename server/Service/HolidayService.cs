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

        var ruleSet = FindRuleSet(month) ?? throw new InvalidOperationException("No active holiday rule set exists for " + month +".");

        var addedWeeks1To5 = AddEarned(
            userId,
            month,
            Weeks1To5,
            weeklyHours,
            ruleSet,
            CalculateWeeks1To5Hours(
                weeklyHours,
                ruleSet.Weeks1To5EarningRate));

        var addedWeek6 = false;

        if (ruleSet.Week6EarningRate > 0)
        {
            addedWeek6 = AddEarned(
                userId,
                month,
                Week6,
                weeklyHours,
                ruleSet,
                CalculateWeek6Hours(
                    weeklyHours,
                    ruleSet.Week6EarningRate));
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
    
    public static HolidayAccount NewAccountFor(string holidayType, string userId, DateOnly month, HolidayRuleSet rules)
{
    var isWeek6 = holidayType == Week6;

    var earningStartDay = isWeek6
        ? rules.Week6EarningStartDay
        : rules.Weeks1To5EarningStartDay;

    var earningStartMonth = isWeek6
        ? rules.Week6EarningStartMonth
        : rules.Weeks1To5EarningStartMonth;

    var earningEndDay = isWeek6
        ? rules.Week6EarningEndDay
        : rules.Weeks1To5EarningEndDay;

    var earningEndMonth = isWeek6
        ? rules.Week6EarningEndMonth
        : rules.Weeks1To5EarningEndMonth;

    var earningEndYearOffset = isWeek6
        ? rules.Week6EarningEndYearOffset
        : rules.Weeks1To5EarningEndYearOffset;

    var earningStartYear = month.Month > earningStartMonth ||
                           (month.Month == earningStartMonth &&
                            month.Day >= earningStartDay)
        ? month.Year
        : month.Year - 1;

    var earningStart = new DateOnly(
        earningStartYear,
        earningStartMonth,
        earningStartDay);

    var earningEnd = new DateOnly(
        earningStartYear + earningEndYearOffset,
        earningEndMonth,
        earningEndDay);

    var usageStart = new DateOnly(
        earningStartYear + (isWeek6
            ? rules.Week6UsageStartYearOffset
            : rules.Weeks1To5UsageStartYearOffset),
        isWeek6
            ? rules.Week6UsageStartMonth
            : rules.Weeks1To5UsageStartMonth,
        isWeek6
            ? rules.Week6UsageStartDay
            : rules.Weeks1To5UsageStartDay);

    var usageEnd = new DateOnly(
        earningStartYear + (isWeek6
            ? rules.Week6UsageEndYearOffset
            : rules.Weeks1To5UsageEndYearOffset),
        isWeek6
            ? rules.Week6UsageEndMonth
            : rules.Weeks1To5UsageEndMonth,
        isWeek6
            ? rules.Week6UsageEndDay
            : rules.Weeks1To5UsageEndDay);

    var transferDeadline = new DateOnly(
        earningStartYear + (isWeek6
            ? rules.Week6TransferDeadlineYearOffset
            : rules.Weeks1To5TransferDeadlineYearOffset),
        isWeek6
            ? rules.Week6TransferDeadlineMonth
            : rules.Weeks1To5TransferDeadlineMonth,
        isWeek6
            ? rules.Week6TransferDeadlineDay
            : rules.Weeks1To5TransferDeadlineDay);

    return new HolidayAccount
    {
        HolidayType = holidayType,
        UserId = userId,
        EarningStart = earningStart,
        EarningEnd = earningEnd,
        UsageStart = usageStart,
        UsageEnd = usageEnd,
        TransferDeadline = transferDeadline
    };
}

    private bool AddEarned(
        string userId,
        DateOnly month,
        string holidayType,
        decimal weeklyHours,
        HolidayRuleSet ruleSet,
        decimal hours)
    {
        var account = GetOrCreateAccount(userId, holidayType, month, ruleSet);

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

            // Reference to the ruleset used for this transaction
            RuleValidFrom = ruleSet.ValidFrom,

            Reason = "Monthly holiday",
            CreatedBy = null,
            CreatedAt = now
        });

        return true;
    }

    private HolidayAccount GetOrCreateAccount(string userId, string holidayType, DateOnly month, HolidayRuleSet ruleSet)
    {
        var template = NewAccountFor(holidayType, userId, month, ruleSet);

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

    private HolidayRuleSet? FindRuleSet(DateOnly date)
    {
        return db.HolidayRuleSets
            .Where(r => r.IsActive && r.ValidFrom <= date)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();
    }
    
    private void EnsureUserExists(string userId)
    {
        if (!db.Users.Any(u => u.UserId == userId))
            throw new NotFoundException("User not found");
    }
}