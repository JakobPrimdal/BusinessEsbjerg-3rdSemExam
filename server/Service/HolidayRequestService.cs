using Infrastructure.Entities;

namespace Service;

//calculates what a request costs
public class HolidayRequestService
{
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

    // The schedule valid on a day: the newest one that started on or before it
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
}