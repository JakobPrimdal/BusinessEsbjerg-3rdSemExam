using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;

public class HolidayRequestService(HolidayDb db, HolidayService holidayService, TimeProvider clock)
{
    // Errors block the request: it can not be sent or approved
    public const string UnknownTypeError = "Ukendt ferietype";
    public const string EndBeforeStartError = "Slutdatoen skal være på eller efter startdatoen";
    public const string StartInPastError = "Ferien kan ikke starte i fortiden";
    public const string TooLongError = "En ferieanmodning må højst være ét år lang";
    public const string NoWorkingDaysError = "Perioden indeholder ingen arbejdsdage";
    public const string OverlapError = "Du har allerede ferie i denne periode";
    public const string DebtLimitError = "Feriegælden må højst være én uge";
    public const string NegativeAtYearEndError = "Saldoen må ikke være negativ den 31. august";
    public const string Week6NotUsableError =
        "Du har ikke nok timer fra 6. ferieuge, som kan bruges i denne periode. " +
        "6. ferieuge kan kun holdes i sin afholdelsesperiode og ikke på forskud";

    // Warnings do not block: the employee can still send the request, and accounting decides
    public const string ShortNoticeWarning = "short_notice";
    public const string MainHolidayWarning = "main_holiday";

    private const int MaxRequestDays = 366;

    // A weeks 1-5 request in the main holiday period with at least this many working days
    // counts as main holiday, so the longer notice period applies
    private const int MainHolidayMinWorkingDays = 10;

    // Ferieloven: 3 weeks of holiday in a row in the main holiday period
    private const int MainHolidayWeeks = 3;

    // Used if no rule set is found
    private const int DefaultMainHolidayNoticeMonths = 3;
    private const int DefaultOtherHolidayNoticeMonths = 1;

    private static readonly string[] DanishMonths =
    [
        "januar", "februar", "marts", "april", "maj", "juni",
        "juli", "august", "september", "oktober", "november", "december"
    ];

    public record RequestCost(int WorkingDays, decimal Hours);

    public static RequestCost CalculateCost(
        DateOnly start,
        DateOnly end,
        IReadOnlyList<WorkHour> schedules,
        IReadOnlySet<DateOnly>? companyDaysOff = null)
    {
        var workingDays = 0;
        decimal hours = 0;

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (companyDaysOff != null && companyDaysOff.Contains(day))
                continue;

            var schedule = ScheduleOn(schedules, day);
            if (schedule is null)
                continue;

            var hoursThatDay = HoursOn(schedule, day.DayOfWeek);
            if (hoursThatDay > 0)
            {
                workingDays++;
                hours += hoursThatDay;
            }
        }

        return new RequestCost(workingDays, HolidayService.RoundHours(hours));
    }

    public static WorkHour? ScheduleOn(IReadOnlyList<WorkHour> schedules, DateOnly day)
        => schedules.LastOrDefault(s => s.ValidFrom <= day);

    public static decimal HoursOn(WorkHour schedule, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => schedule.MonHours,
        DayOfWeek.Tuesday => schedule.TueHours,
        DayOfWeek.Wednesday => schedule.WedHours,
        DayOfWeek.Thursday => schedule.ThuHours,
        DayOfWeek.Friday => schedule.FriHours,
        _ => 0m
    };

    public static decimal WeekHours(WorkHour schedule)
        => HolidayService.RoundHours(
            HoursOn(schedule, DayOfWeek.Monday) + HoursOn(schedule, DayOfWeek.Tuesday) +
            HoursOn(schedule, DayOfWeek.Wednesday) + HoursOn(schedule, DayOfWeek.Thursday) +
            HoursOn(schedule, DayOfWeek.Friday));

    // Mon-Fri days with hours. Full time = 5.
    public static int WorkingDaysPerWeek(WorkHour schedule)
        => new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            .Count(d => HoursOn(schedule, d) > 0);

    // The main holiday period in a given year, from the rule set (default 1 May - 30 September)
    public static (DateOnly Start, DateOnly End) MainHolidayPeriod(HolidayRuleSet rules, int year)
        => (new DateOnly(year, rules.MainHolidayStartMonth, rules.MainHolidayStartDay),
            new DateOnly(year, rules.MainHolidayEndMonth, rules.MainHolidayEndDay));

    public static bool Overlaps(DateOnly start, DateOnly end, DateOnly periodStart, DateOnly periodEnd)
        => start <= periodEnd && end >= periodStart;

    // Main holiday has a longer notice period than other holiday
    public static bool IsMainHoliday(string holidayType, DateOnly start, DateOnly end, int workingDays,
        (DateOnly Start, DateOnly End) mainPeriod)
        => holidayType == HolidayService.Weeks1To5
           && Overlaps(start, end, mainPeriod.Start, mainPeriod.End)
           && workingDays >= MainHolidayMinWorkingDays;

    public static bool IsShortNotice(DateOnly madeOn, DateOnly start, int noticeMonths)
        => start < madeOn.AddMonths(noticeMonths);

    // The most working days in a row that are covered by holiday, inside [from, to].
    // Weekends and other days without work hours do not break a run, a working day without holiday does.
    public static int LongestHolidayRun(
        IEnumerable<(DateOnly Start, DateOnly End)> holidays,
        DateOnly from,
        DateOnly to,
        IReadOnlyList<WorkHour> schedules)
    {
        var covered = new HashSet<DateOnly>();
        foreach (var (start, end) in holidays)
        {
            for (var day = start > from ? start : from; day <= end && day <= to; day = day.AddDays(1))
                covered.Add(day);
        }

        int longest = 0, current = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var schedule = ScheduleOn(schedules, day);
            if (schedule is null || HoursOn(schedule, day.DayOfWeek) == 0)
                continue; // not a working day

            current = covered.Contains(day) ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }
        return longest;
    }

    // "1. maj" (does not depend on the server's culture settings)
    public static string DanishDate(DateOnly date) => $"{date.Day}. {DanishMonths[date.Month - 1]}";

    // Shows the cost, the balance, errors and warnings. Saves nothing.
    public HolidayRequestPreviewDTO Preview(string userId, CreateHolidayRequestDTO dto)
    {
        try
        {
            var check = Check(userId, dto);
            return new HolidayRequestPreviewDTO(
                check.Cost.WorkingDays,
                check.Cost.Hours,
                check.BalanceBefore,
                check.BalanceAfter,
                check.MaxDebt,
                CanRequest: check.Error is null,
                check.Error,
                check.Warnings);
        }
        catch (BadRequestException ex)
        {
            return new HolidayRequestPreviewDTO(0, 0, 0, 0, 0, CanRequest: false, ex.Message, []);
        }
    }

    // Saves the request as pending, after the same checks as the preview.
    // Warnings do not stop it. The balance is not changed: hours are only used when the accountant approves.
    public HolidayRequestDTO Create(string userId, CreateHolidayRequestDTO dto)
    {
        var check = Check(userId, dto);
        if (check.Error != null)
            throw new BadRequestException(check.Error);

        var request = new HolidayRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HolidayType = dto.HolidayType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Hours = check.Cost.Hours,
            Status = HolidayRequestStatus.Pending,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        db.Insert(request);

        return new HolidayRequestDTO(
            request.Id,
            request.HolidayType,
            request.StartDate,
            request.EndDate,
            request.Hours,
            request.Status,
            request.CreatedAt,
            check.Warnings);
    }

    private record CheckResult(
        RequestCost Cost, decimal BalanceBefore, decimal BalanceAfter, decimal MaxDebt, string? Error,
        List<HolidayRuleWarningDTO> Warnings);

    private CheckResult Check(string userId, CreateHolidayRequestDTO dto)
    {
        if (dto.HolidayType is not (HolidayService.Weeks1To5 or HolidayService.Week6))
            throw new BadRequestException(UnknownTypeError);
        if (dto.EndDate < dto.StartDate)
            throw new BadRequestException(EndBeforeStartError);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (dto.StartDate < today)
            throw new BadRequestException(StartInPastError);
        if (dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1 > MaxRequestDays)
            throw new BadRequestException(TooLongError);

        var schedules = SchedulesFor(userId);

        var cost = CalculateCost(dto.StartDate, dto.EndDate, schedules);
        if (cost.Hours == 0)
            throw new BadRequestException(NoWorkingDaysError);

        var overlaps = db.HolidayRequests.Any(r =>
            r.UserId == userId
            && HolidayRequestStatus.Active.Contains(r.Status)
            && r.StartDate <= dto.EndDate
            && r.EndDate >= dto.StartDate);
        if (overlaps)
            throw new BadRequestException(OverlapError);

        var rules = CheckHolidayRules(userId, dto.HolidayType, dto.StartDate, cost.Hours, schedules);
        var warnings = CheckWarnings(userId, dto.HolidayType, dto.StartDate, dto.EndDate, cost.WorkingDays,
            schedules, madeOn: today);

        return new CheckResult(cost, rules.BalanceBefore, rules.BalanceAfter, rules.MaxDebt, rules.Error, warnings);
    }

    public record RuleCheck(decimal BalanceBefore, decimal BalanceAfter, decimal MaxDebt, string? Error);

    // The rules that block a request. Also used when the accountant approves.
    public RuleCheck CheckHolidayRules(string userId, string holidayType, DateOnly start, decimal hours,
        IReadOnlyList<WorkHour> schedules, Guid? existingRequestId = null)
    {
        var balanceBefore = holidayService.ProjectedBalance(userId, holidayType, start, leaveOut: existingRequestId);
        var balanceAfter = HolidayService.RoundHours(balanceBefore - hours);

        // Week 6 can only be held in its usage period and not in advance, so there is no debt:
        // the balance only counts week 6 hours that are usable on the start date
        if (holidayType == HolidayService.Week6)
        {
            return balanceAfter < 0
                ? new RuleCheck(balanceBefore, balanceAfter, 0, Week6NotUsableError)
                : new RuleCheck(balanceBefore, balanceAfter, 0, null);
        }

        // Weeks 1-5: at most one week of debt, and not negative at the end of the holiday year
        var scheduleAtStart = ScheduleOn(schedules, start) ?? schedules[0];
        var maxDebt = WeekHours(scheduleAtStart);

        if (balanceAfter < -maxDebt)
            return new RuleCheck(balanceBefore, balanceAfter, maxDebt, DebtLimitError);

        var endOfYear = HolidayService.EndOfHolidayYear(start);
        var atEndOfYear = holidayService.ProjectedBalance(userId, holidayType, endOfYear.AddDays(1),
            notSavedYet: new HolidayService.PlannedUse(start, hours), leaveOut: existingRequestId);
        if (atEndOfYear < 0)
            return new RuleCheck(balanceBefore, balanceAfter, maxDebt, NegativeAtYearEndError);

        return new RuleCheck(balanceBefore, balanceAfter, maxDebt, null);
    }

    // The rules that only warn. madeOn is the day the request was (or is being) made, for the notice period.
    // existingRequestId leaves a saved request out of the plan, so it is not counted twice.
    public List<HolidayRuleWarningDTO> CheckWarnings(string userId, string holidayType, DateOnly start, DateOnly end,
        int workingDays, IReadOnlyList<WorkHour> schedules, DateOnly madeOn, Guid? existingRequestId = null)
    {
        var warnings = new List<HolidayRuleWarningDTO>();
        var rules = holidayService.RuleSetOn(start);
        (DateOnly Start, DateOnly End)? mainPeriod = rules is null ? null : MainHolidayPeriod(rules, start.Year);

        // Notice period: main holiday must be announced earlier than other holiday
        var isMain = mainPeriod is { } period && IsMainHoliday(holidayType, start, end, workingDays, period);
        var noticeMonths = isMain
            ? NoticeMonths(HolidayService.NoticeMainHolidayMonthsKey, madeOn, DefaultMainHolidayNoticeMonths)
            : NoticeMonths(HolidayService.NoticeOtherHolidayMonthsKey, madeOn, DefaultOtherHolidayNoticeMonths);
        if (IsShortNotice(madeOn, start, noticeMonths))
        {
            warnings.Add(new HolidayRuleWarningDTO(ShortNoticeWarning,
                $"Kort varsel: {(isMain ? "hovedferie" : "ferie")} skal varsles mindst {noticeMonths} " +
                $"{(noticeMonths == 1 ? "måned" : "måneder")} før den starter."));
        }

        // Main holiday: 3 weeks in a row in the main holiday period. Only weeks 1-5 counts,
        // and only requests that touch the period, so a request in February does not warn about the summer.
        if (holidayType == HolidayService.Weeks1To5
            && mainPeriod is { } main
            && Overlaps(start, end, main.Start, main.End))
        {
            var plan = db.HolidayRequests
                .Where(r => r.UserId == userId
                            && r.HolidayType == HolidayService.Weeks1To5
                            && HolidayRequestStatus.Active.Contains(r.Status)
                            && r.StartDate <= main.End
                            && r.EndDate >= main.Start)
                .Select(r => new { r.Id, r.StartDate, r.EndDate })
                .ToList()
                .Where(r => r.Id != existingRequestId)
                .Select(r => (r.StartDate, r.EndDate))
                .Append((start, end))
                .ToList();

            var schedule = ScheduleOn(schedules, main.Start) ?? ScheduleOn(schedules, start);
            var required = schedule is null ? 0 : MainHolidayWeeks * WorkingDaysPerWeek(schedule);
            var longest = LongestHolidayRun(plan, main.Start, main.End, schedules);

            if (required > 0 && longest < required)
            {
                warnings.Add(new HolidayRuleWarningDTO(MainHolidayWarning,
                    $"Hovedferien er ikke {MainHolidayWeeks} uger i træk: din plan giver {longest} af {required} " +
                    $"arbejdsdage i træk mellem {DanishDate(main.Start)} og {DanishDate(main.End)}."));
            }
        }

        return warnings;
    }

    // employee's work schedules, oldest first
    public List<WorkHour> SchedulesFor(string userId)
        => db.WorkHours
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.ValidFrom)
            .ThenBy(w => w.CreatedAt)
            .ToList();

    private int NoticeMonths(string ruleKey, DateOnly date, int defaultMonths)
    {
        var rule = holidayService.RuleValueOn(ruleKey, date);
        return rule is null ? defaultMonths : (int)rule.Value;
    }
}