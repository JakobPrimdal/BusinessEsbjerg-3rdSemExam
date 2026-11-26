using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;

public class HolidayService(HolidayDb db, TimeProvider clock)
{

    public const string EarnedType = "earned";     // + holiday earned in a month (the monthly job)
    public const string UsageType = "usage";       // - holiday taken: written when a request is approved
    public const string ReversalType = "reversal"; // + undoes a usage line (cancelled approved holiday, later)

    public const string Weeks1To5 = "weeks_1_5";
    public const string Week6 = "week_6";
    
    // Notice periods for holiday requests
    public const string NoticeMainHolidayMonthsKey = "notice_main_holiday_months";
    public const string NoticeOtherHolidayMonthsKey = "notice_other_holiday_months";

    private const decimal FullTimeWeeklyHours = 37m;
    
    public const int HourDecimals = 2;
    public const string EmployeeOnlyError = "Work hours can only be added to employee accounts"; 
    
    public static decimal RoundHours(decimal hours)
        => Math.Round(hours, HourDecimals, MidpointRounding.AwayFromZero);

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

        var ruleSet = FindRuleSet(month)
                      ?? throw new InvalidOperationException(
                          $"No active holiday rule set exists for {month}.");

        var earning = CalculateMonth(userId, month, ruleSet);

        if (earning is null)
            return false;

        var addedWeeks1To5 = AddEarned(
            userId,
            month,
            Weeks1To5,
            earning.WeeklyHours,
            ruleSet,
            earning.Weeks1To5Hours);

        var addedWeek6 = false;

        if (ruleSet.Week6EarningRate > 0)
        {
            addedWeek6 = AddEarned(
                userId,
                month,
                Week6,
                earning.WeeklyHours,
                ruleSet,
                earning.Week6Hours);
        }

        return addedWeeks1To5 || addedWeek6;
    }
    
    // 'leaveOut' is a saved request that must not be counted, because it is the one being checked
    // (the accountant approving a pending request: it is added as 'notSavedYet' instead).
    public decimal ProjectedBalance(string userId, string holidayType, DateOnly date,
        PlannedUse? notSavedYet = null, Guid? leaveOut = null)
    {
        var earned = EarnedLots(userId, holidayType, date);

        // Approved holiday comes from the ledger (usage lines), pending holiday from the requests.
        // A request is in one of the two, never both, so nothing is counted twice.
        var planned = ApprovedUses(userId, holidayType);
        planned.AddRange(db.HolidayRequests
            .Where(r => r.UserId == userId
                        && r.HolidayType == holidayType
                        && r.Status == HolidayRequestStatus.Pending
                        && r.StartDate < date)
            .Select(r => new { r.Id, r.StartDate, r.Hours })
            .ToList()
            .Where(r => r.Id != leaveOut)
            .Select(r => new PlannedUse(r.StartDate, r.Hours)));
        if (notSavedYet != null)
            planned.Add(notSavedYet);

        return CalculateBalance(earned, planned, date);
    }

    public void RecordUsage(HolidayRequest request, string decidedBy)
    {
        // Lots up to 14 months ahead, to find the account that pays back debt
        // (week 6: the next 1 May; weeks 1-5: the next month)
        var earned = EarnedLots(request.UserId, request.HolidayType, request.StartDate.AddMonths(14));

        var ruleSet = FindRuleSet(request.StartDate) ?? throw new InvalidOperationException("No active holiday rule set exists for " + request.StartDate + ".");

        var debtAccount = earned
          .Where(l => l.UsableFrom > request.StartDate)
          .OrderBy(l => l.UsableFrom)
          .Select(l => (DateOnly?)l.AccountStart)
          .FirstOrDefault()
           ?? NewAccountFor(request.HolidayType, request.UserId, request.StartDate, ruleSet).EarningStart;
        
        var parts = AllocateUse(earned, ApprovedUses(request.UserId, request.HolidayType),
            new PlannedUse(request.StartDate, request.Hours), debtAccount);

        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var part in parts)
        {
            var account = GetOrCreateAccount(request.UserId, request.HolidayType, part.AccountStart, ruleSet);
            
            db.Insert(new HolidayTransaction
            {
                Id = Guid.NewGuid(),
                HolidayAccountId = account.Id,
                UserId = request.UserId,
                Type = UsageType,
                Hours = -part.Hours,
                TransactionDate = request.StartDate,
                PeriodMonth = null,
                Reason = $"Holiday {request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}",
                CreatedBy = decidedBy,
                HolidayRequestId = request.Id,
                CreatedAt = now
            });
        }
    }
    
    // gives back the hours of an approved request when its cancellation is approved
    public decimal RecordReversal(HolidayRequest request, string decidedBy)
    {
        var lines = db.HolidayTransactions
            .Where(t => t.HolidayRequestId == request.Id && (t.Type == UsageType || t.Type == ReversalType))
            .ToList();

        var now = clock.GetUtcNow().UtcDateTime;
        decimal returned = 0;
        foreach (var perAccount in lines.GroupBy(t => t.HolidayAccountId))
        {
            // what is still used on this account (a usage line is negative)
            var stillUsed = -perAccount.Sum(t => t.Hours);
            if (stillUsed <= 0)
                continue;

            db.Insert(new HolidayTransaction
            {
                Id = Guid.NewGuid(),
                HolidayAccountId = perAccount.Key,
                UserId = request.UserId,
                Type = ReversalType,
                Hours = stillUsed,
                TransactionDate = request.StartDate,
                PeriodMonth = null,
                Reason = $"Cancelled holiday {request.StartDate:yyyy-MM-dd} - {request.EndDate:yyyy-MM-dd}",
                CreatedBy = decidedBy,
                HolidayRequestId = request.Id,
                CreatedAt = now
            });
            returned += stillUsed;
        }
        return RoundHours(returned);
    }

    private List<PlannedUse> ApprovedUses(string userId, string holidayType)
        => (from t in db.HolidayTransactions
            join a in db.HolidayAccounts on t.HolidayAccountId equals a.Id
            where t.UserId == userId
                  && a.HolidayType == holidayType
                  && (t.Type == UsageType || t.Type == ReversalType)
                  && t.HolidayRequestId != null
            select new { RequestId = t.HolidayRequestId!.Value, t.TransactionDate, t.Hours })
            .ToList()
            .GroupBy(t => t.RequestId)
            .Select(g => new PlannedUse(g.Min(t => t.TransactionDate), -g.Sum(t => t.Hours)))
            .Where(p => p.Hours > 0)
            .ToList();
    
    public decimal EarnedHoursForMonth(string userId, string holidayType, DateOnly anyDayInMonth)
    {
        var month = FirstDayOfMonth(anyDayInMonth);
        return SavedAccruals(userId, holidayType).TryGetValue(month, out var saved)
            ? saved
            : CalculatedHoursForMonth(userId, holidayType, month);
    }

    // One lot per month the employee has worked, for months whose holiday is usable on or before 'date'
    private List<EarnedLot> EarnedLots(
        string userId,
        string holidayType,
        DateOnly date)
    {
        var firstSchedule = db.WorkHours
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.ValidFrom)
            .FirstOrDefault();

        if (firstSchedule is null)
            return [];

        var saved = SavedAccruals(userId, holidayType);
        var lots = new List<EarnedLot>();

        for (var month = FirstDayOfMonth(firstSchedule.ValidFrom);
             month <= PreviousMonth(date);
             month = month.AddMonths(1))
        {
            var ruleSet = FindRuleSet(month);

            if (ruleSet is null)
                continue;

            var account = NewAccountFor(
                holidayType,
                userId,
                month,
                ruleSet);

            var monthOver = month.AddMonths(1);

            var usableFrom = monthOver > account.UsageStart
                ? monthOver
                : account.UsageStart;

            if (usableFrom > date)
                continue;

            var hours = saved.TryGetValue(month, out var savedHours)
                ? savedHours
                : CalculatedHoursForMonth(userId, holidayType, month);

            if (hours > 0)
            {
                lots.Add(new EarnedLot(
                    usableFrom,
                    account.UsageEnd,
                    hours,
                    account.EarningStart));
            }
        }

        return lots;
    }

    private Dictionary<DateOnly, decimal> SavedAccruals(string userId, string holidayType)
        => (from t in db.HolidayTransactions
            join a in db.HolidayAccounts on t.HolidayAccountId equals a.Id
            where t.UserId == userId
                  && t.Type == EarnedType
                  && t.PeriodMonth != null
                  && a.HolidayType == holidayType
            select new { Month = t.PeriodMonth!.Value, t.Hours })
            .ToList()
            .GroupBy(x => x.Month)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Hours));

    private decimal CalculatedHoursForMonth(
        string userId,
        string holidayType,
        DateOnly month)
    {
        var ruleSet = FindRuleSet(month);

        if (ruleSet is null)
            return 0;

        var earning = CalculateMonth(userId, month, ruleSet);

        if (earning is null)
            return 0;

        return holidayType == Week6
            ? earning.Week6Hours
            : earning.Weeks1To5Hours;
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
        
        if (!db.Users.Any(u => u.UserId == dto.UserId && u.Role == UserRole.User))
            throw new BadRequestException(EmployeeOnlyError);
        
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
        => Math.Round(daysPerMonth * weeklyHours / 5m, HourDecimals, MidpointRounding.AwayFromZero);

    public static decimal CalculateWeek6Hours(decimal weeklyHours, decimal fullTimeHoursPerMonth)
        => Math.Round(fullTimeHoursPerMonth * weeklyHours / FullTimeWeeklyHours, HourDecimals, MidpointRounding.AwayFromZero);

    public static decimal WeeklyHours(WorkHour w)
        => w.MonHours + w.TueHours + w.WedHours + w.ThuHours + w.FriHours + w.SatHours + w.SunHours;

    public static DateOnly HolidayYearStart(DateOnly date)
        => new(date.Month >= 9 ? date.Year : date.Year - 1, 9, 1);

    public static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
    public static DateOnly PreviousMonth(DateOnly today) => FirstDayOfMonth(today).AddMonths(-1);
    
    public static DateOnly EndOfHolidayYear(DateOnly date)
        => HolidayYearStart(date).AddYears(1).AddDays(-1);
    
    public record EarnedLot(DateOnly UsableFrom, DateOnly UsableTo, decimal Hours, DateOnly AccountStart = default);

    // Part of a holiday taken from one account
    public record UsePart(DateOnly AccountStart, decimal Hours);

    // Holiday that is planned (pending or approved) and uses hours from the day it starts
    public record PlannedUse(DateOnly StartDate, decimal Hours);
    
    private sealed class OpenLot(DateOnly usableTo, decimal hours, DateOnly accountStart)
    {
        public DateOnly UsableTo { get; } = usableTo;
        public decimal Hours { get; set; } = hours;
        public DateOnly AccountStart { get; } = accountStart;
    }
    public static decimal CalculateBalance(
        IEnumerable<EarnedLot> earned, IEnumerable<PlannedUse> planned, DateOnly date)
    {
        var (open, debt) = Simulate(earned, planned, date);
        open.RemoveAll(l => l.UsableTo < date);
        return RoundHours(open.Sum(l => l.Hours) - debt);
    }

    public static List<UsePart> AllocateUse(
        IEnumerable<EarnedLot> earned, IEnumerable<PlannedUse> earlier, PlannedUse use, DateOnly debtAccount)
    {
        var (open, _) = Simulate(earned, earlier, use.StartDate);
        open.RemoveAll(l => l.UsableTo < use.StartDate);

        var parts = new List<UsePart>();
        var needed = use.Hours;
        foreach (var lot in open.OrderBy(l => l.UsableTo))
        {
            if (needed == 0)
                break;
            var take = Math.Min(needed, lot.Hours);
            if (take > 0)
                parts.Add(new UsePart(lot.AccountStart, take));
            needed -= take;
        }
        if (needed > 0)
            parts.Add(new UsePart(debtAccount, needed));

        // One part per account
        return parts
            .GroupBy(p => p.AccountStart)
            .Select(g => new UsePart(g.Key, RoundHours(g.Sum(p => p.Hours))))
            .ToList();
    }
    
    private static (List<OpenLot> Open, decimal Debt) Simulate(
        IEnumerable<EarnedLot> earned, IEnumerable<PlannedUse> planned, DateOnly date)
    {
        var events = earned
            .Where(l => l.UsableFrom <= date)
            .Select(l => (Date: l.UsableFrom, Order: 0, Lot: l, Use: (PlannedUse?)null))
            .Concat(planned
                .Where(p => p.StartDate < date)
                .Select(p => (Date: p.StartDate, Order: 1, Lot: (EarnedLot?)null, Use: (PlannedUse?)p)))
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Order);

        var open = new List<OpenLot>();
        decimal debt = 0;

        foreach (var e in events)
        {
            open.RemoveAll(l => l.UsableTo < e.Date); // expired

            if (e.Lot != null)
            {
                var payBack = Math.Min(debt, e.Lot.Hours);
                debt -= payBack;
                if (e.Lot.Hours - payBack > 0)
                    open.Add(new OpenLot(e.Lot.UsableTo, e.Lot.Hours - payBack, e.Lot.AccountStart));
            }
            else
            {
                var needed = e.Use!.Hours;
                foreach (var lot in open.OrderBy(l => l.UsableTo))
                {
                    var take = Math.Min(needed, lot.Hours);
                    lot.Hours -= take;
                    needed -= take;
                    if (needed == 0)
                        break;
                }
                open.RemoveAll(l => l.Hours == 0);
                debt += needed;
            }
        }

        return (open, debt);
    }

    // One work schedule and how many calendar days of a month it covers
    public record SchedulePeriod(decimal WeeklyHours, int Days);

    // Splits a month into the periods each work schedule covers.
    // Days before the first schedule (not started yet) are not included.
    // A schedule with 0 hours gives a period with 0 weekly hours.
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
    
    public static decimal ProRate(decimal fullMonthHours, int daysWorked, int daysInMonth)
        => RoundHours(fullMonthHours * daysWorked / daysInMonth);
    
    public static HolidayAccount NewAccountFor(
    string holidayType,
    string userId,
    DateOnly month,
    HolidayRuleSet rules)
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

    var earningStartYear = month.Month >= earningStartMonth
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
        isWeek6 ? rules.Week6UsageStartMonth : rules.Weeks1To5UsageStartMonth,
        isWeek6 ? rules.Week6UsageStartDay : rules.Weeks1To5UsageStartDay);

    var usageEnd = new DateOnly(
        earningStartYear + (isWeek6
            ? rules.Week6UsageEndYearOffset
            : rules.Weeks1To5UsageEndYearOffset),
        isWeek6 ? rules.Week6UsageEndMonth : rules.Weeks1To5UsageEndMonth,
        isWeek6 ? rules.Week6UsageEndDay : rules.Weeks1To5UsageEndDay);

    var transferDeadline = new DateOnly(
        earningStartYear + (isWeek6
            ? rules.Week6TransferDeadlineYearOffset
            : rules.Weeks1To5TransferDeadlineYearOffset),
        isWeek6 ? rules.Week6TransferDeadlineMonth : rules.Weeks1To5TransferDeadlineMonth,
        isWeek6 ? rules.Week6TransferDeadlineDay : rules.Weeks1To5TransferDeadlineDay);

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
    
    // What a user earns in one month, before it is saved
    private record MonthEarning(
        decimal WeeklyHours,
        decimal Weeks1To5Hours,
        decimal Week6Hours);

    // Calculates the holiday for one month. Each work schedule counts for the calendar days it covers,
    // so starting, stopping or changing hours in the middle of a month earns only for those days.
    private MonthEarning? CalculateMonth(
        string userId,
        DateOnly month,
        HolidayRuleSet ruleSet)
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

        var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

        decimal weeks1To5Hours = 0;
        decimal week6Hours = 0;

        foreach (var period in periods)
        {
            weeks1To5Hours += ProRate(
                CalculateWeeks1To5Hours(
                    period.WeeklyHours,
                    ruleSet.Weeks1To5EarningRate),
                period.Days,
                daysInMonth);

            if (ruleSet.Week6EarningRate > 0)
            {
                week6Hours += ProRate(
                    CalculateWeek6Hours(
                        period.WeeklyHours,
                        ruleSet.Week6EarningRate),
                    period.Days,
                    daysInMonth);
            }
        }

        return new MonthEarning(
            WeeklyHours(schedules[^1]),
            weeks1To5Hours,
            week6Hours);
    }

    public HolidayRuleSet? RuleSetOn(DateOnly date) => FindRuleSet(date);
    
    public decimal? RuleValueOn(string key, DateOnly date)
    {
        var ruleSet = FindRuleSet(date);

        if (ruleSet is null)
            return null;

        return key switch
        {
            NoticeMainHolidayMonthsKey => ruleSet.MainHolidayNoticePeriodMonths,
            NoticeOtherHolidayMonthsKey => ruleSet.OtherHolidayNoticePeriodMonths,
            _ => null
        };
    }
    
    private void EnsureUserExists(string userId)
    {
        if (!db.Users.Any(u => u.UserId == userId))
            throw new NotFoundException("User not found");
    }
}