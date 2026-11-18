using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;

public class HolidayRequestService(HolidayDb db, HolidayService holidayService, TimeProvider clock)
{
    public const string UnknownTypeError = "Ukendt ferietype";
    public const string EndBeforeStartError = "Slutdatoen skal være på eller efter startdatoen";
    public const string StartInPastError = "Ferien kan ikke starte i fortiden";
    public const string TooLongError = "En ferieanmodning må højst være ét år lang";
    public const string NoWorkingDaysError = "Perioden indeholder ingen arbejdsdage";
    public const string OverlapError = "Du har allerede ferie i denne periode";
    public const string DebtLimitError = "Feriegælden må højst være én uge";
    public const string NegativeAtYearEndError = "Saldoen må ikke være negativ den 31. august";
    private const int MaxRequestDays = 366;
    
    private const int MainHolidayMinWorkingDays = 10;
    
    
    // Used if the notice rules are missing in the database 
    private const int DefaultMainHolidayNoticeMonths = 3;
    private const int DefaultOtherHolidayNoticeMonths = 1;
    
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
    
    // Main holiday has a longer notice period (3 months) than other holiday (1 month)
    public static bool IsMainHoliday(string holidayType, DateOnly start, int workingDays)
        => holidayType == HolidayService.Weeks1To5
           && start.Month is >= 5 and <= 9
           && workingDays >= MainHolidayMinWorkingDays;
    
    public static bool IsShortNotice(DateOnly today, DateOnly start, int noticeMonths)
        => start < today.AddMonths(noticeMonths);
    
    // shows the cost and the balance, and whether the request is allowed. Saves nothing
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
                check.ShortNotice,
                check.NoticeMonths);
        }
        catch (BadRequestException ex)
        {
            return new HolidayRequestPreviewDTO(
                0, 0, 0, 0, 0,
                CanRequest: false,
                ex.Message,
                ShortNotice: false,
                NoticeMonths: 0);}
    }

    // Saves the request as pending, after the same checks as the preview
    // it does not change the balance as hours are only used when the accountant approves
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
            check.ShortNotice,
            check.NoticeMonths);
    }
 
    private record CheckResult(
        RequestCost Cost, decimal BalanceBefore, decimal BalanceAfter, decimal MaxDebt, string? Error,
        bool ShortNotice, int NoticeMonths);

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
        
        var schedules = db.WorkHours
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.ValidFrom)
            .ThenBy(w => w.CreatedAt)
            .ToList();
        
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
        
        //  the balance on the start date may be at most one week negative
        var balanceBefore = holidayService.ProjectedBalance(userId, dto.HolidayType, dto.StartDate);
        var balanceAfter = HolidayService.RoundHours(balanceBefore - cost.Hours);

        var scheduleAtStart = ScheduleOn(schedules, dto.StartDate) ?? schedules[0];
        var maxDebt = WeekHours(scheduleAtStart);

        string? error = null;
        if (balanceAfter < -maxDebt)
        {
            error = DebtLimitError;
        }
        else
        {
            //  balance at the end of 31 August may not be negative.
            //   everything earned until the end of August,
            //   minus all holiday planned before 1 September, including this request
            var endOfYear = HolidayService.EndOfHolidayYear(dto.StartDate);
            var atEndOfYear = holidayService.ProjectedBalance(userId, dto.HolidayType, endOfYear.AddDays(1),
                notSavedYet: new HolidayService.PlannedUse(dto.StartDate, cost.Hours));
            if (atEndOfYear < 0)
                error = NegativeAtYearEndError;
        }
        
        //  notice rules in start today when the request is made
        var noticeMonths = IsMainHoliday(dto.HolidayType, dto.StartDate, cost.WorkingDays)
            ? NoticeMonths(HolidayService.NoticeMainHolidayMonthsKey, today, DefaultMainHolidayNoticeMonths)
            : NoticeMonths(HolidayService.NoticeOtherHolidayMonthsKey, today, DefaultOtherHolidayNoticeMonths);
        var shortNotice = IsShortNotice(today, dto.StartDate, noticeMonths);

        return new CheckResult(cost, balanceBefore, balanceAfter, maxDebt, error, shortNotice, noticeMonths);
    }
       
    private int NoticeMonths(string ruleKey, DateOnly today, int defaultMonths)
    {
        var rule = holidayService.RuleValueOn(ruleKey, today);
        return rule is null ? defaultMonths : (int)rule.Value;
    }
}