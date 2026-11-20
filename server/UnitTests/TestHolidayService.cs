using Infrastructure.Entities;
using Service;

namespace UnitTests;

public class TestHolidayService
{
  
    [Fact]
    public void CalculateWeeks1To5Hours_FullTime37Hours_Returns15Point39()
    {
        // Arrange
        decimal weeklyHours = 37m;
        decimal daysPerMonth = 2.08m;

        // Act
        decimal result = HolidayService.CalculateWeeks1To5Hours(weeklyHours, daysPerMonth);

        // Assert: 2.08 * 37 / 5 = 15.392, rounded to 15.39
        Assert.Equal(15.392m, result);
    }

    [Fact]
    public void CalculateWeeks1To5Hours_ZeroHours_ReturnsZero()
    {
        decimal result = HolidayService.CalculateWeeks1To5Hours(0m, 2.08m);

        Assert.Equal(0m, result);
    }

    // 6th holiday week 
    // Rule: a full-time  (37 hours) gets the full value and part-time gets a share of it

    [Fact]
    public void CalculateWeek6Hours_FullTime_ReturnsFullRuleValue()
    {
        decimal result = HolidayService.CalculateWeek6Hours(37m, 3.083m);
        
        Assert.Equal(3.083m, result);
    }

    [Fact]
    public void CalculateWeek6Hours_HalfTime_ReturnsHalfOfRuleValue()
    {
        decimal result = HolidayService.CalculateWeek6Hours(18.5m, 3.083m);

        // 3.083 / 2 = 1.5415
        Assert.Equal(1.5415m, result);
    }

    // Weekly hours  

    [Fact]
    public void WeeklyHours_FiveDaysOf7Point4Hours_Returns37()
    {
        // Arrange
        var workHour = new WorkHour
        {
            MonHours = 7.4m, TueHours = 7.4m, WedHours = 7.4m, ThuHours = 7.4m, FriHours = 7.4m
        };

        // Act
        decimal result = HolidayService.WeeklyHours(workHour);

        // Assert
        Assert.Equal(37m, result);
    }
    
    // The holiday year runs from 1 September to 31 August.

    [Theory]
    [InlineData(2026, 8, 31, 2025)] // 31 August still belongs to the year that started last September
    [InlineData(2026, 9, 1, 2026)]  // 1 September starts a new holiday year
    public void HolidayYearStart_ReturnsFirstOfSeptember(int year, int month, int day, int expectedYear)
    {
        DateOnly result = HolidayService.HolidayYearStart(new DateOnly(year, month, day));

        Assert.Equal(new DateOnly(expectedYear, 9, 1), result);
    }

    [Fact]
    public void PreviousMonth_InJanuary_ReturnsDecemberOfLastYear()
    {
        DateOnly result = HolidayService.PreviousMonth(new DateOnly(2026, 1, 10));

        Assert.Equal(new DateOnly(2025, 12, 1), result);
    }
    
    public static TheoryData<decimal, decimal> RoundHoursCases => new()
    {
        { 15.392m, 15.39m }, // rounds down
        { 2.346m, 2.35m },   // rounds up
        { 1.005m, 1.01m },   // exactly in the middle 
        { 7m, 7m }
    };

    [Theory]
    [MemberData(nameof(RoundHoursCases))]
    public void RoundHours_KeepsTwoDecimals(decimal hours, decimal expected)
    {
        Assert.Equal(expected, HolidayService.RoundHours(hours));
    }
    
    private static WorkHour Schedule(DateOnly validFrom, decimal hoursPerDay) => new()
    {
        ValidFrom = validFrom,
        MonHours = hoursPerDay, TueHours = hoursPerDay, WedHours = hoursPerDay,
        ThuHours = hoursPerDay, FriHours = hoursPerDay
    };

    [Fact]
    public void DaysPerSchedule_ScheduleFromBeforeTheMonth_CoversWholeMonth()
    {
        // Arrange: full time since January
        var schedules = new List<WorkHour> { Schedule(new DateOnly(2026, 1, 1), 7.4m) };

        // Act
        var periods = HolidayService.DaysPerSchedule(schedules, new DateOnly(2026, 3, 1));

        // Assert: all 31 days of March
        Assert.Equal(new[] { new HolidayService.SchedulePeriod(37m, 31) }, periods);
    }

    [Fact]
    public void DaysPerSchedule_StartedOn20March_CountsOnly12Days()
    {
        var schedules = new List<WorkHour> { Schedule(new DateOnly(2026, 3, 20), 7.4m) };

        var periods = HolidayService.DaysPerSchedule(schedules, new DateOnly(2026, 3, 1));

        // 20-31 March = 12 days
        Assert.Equal(new[] { new HolidayService.SchedulePeriod(37m, 12) }, periods);
    }

    [Fact]
    public void DaysPerSchedule_HoursChangedOn16March_SplitsTheMonth()
    {
        // Arrange: full time, then half time from 16 March
        var schedules = new List<WorkHour>
        {
            Schedule(new DateOnly(2026, 1, 1), 7.4m),
            Schedule(new DateOnly(2026, 3, 16), 3.7m)
        };

        // Act
        var periods = HolidayService.DaysPerSchedule(schedules, new DateOnly(2026, 3, 1));

        // Assert: 1-15 March full time, 16-31 March half time
        Assert.Equal(
            new[] { new HolidayService.SchedulePeriod(37m, 15), new HolidayService.SchedulePeriod(18.5m, 16) },
            periods);
    }

    [Fact]
    public void DaysPerSchedule_StartsNextMonth_ReturnsNoPeriods()
    {
        var schedules = new List<WorkHour> { Schedule(new DateOnly(2026, 4, 1), 7.4m) };

        var periods = HolidayService.DaysPerSchedule(schedules, new DateOnly(2026, 3, 1));

        Assert.Empty(periods);
    }

    [Theory]
    [InlineData(31, 31, 10)]   // whole month -everything
    [InlineData(15, 30, 5)]    // half the month - half
    [InlineData(10, 31, 3.23)] // 10 * 10 / 31 = 3.2258 - rounded to 2 decimals
    [InlineData(0, 31, 0)]     // no days -nothing
    public void ProRate_ReturnsShareOfTheMonth(int daysWorked, int daysInMonth, double expected)
    {
        decimal result = HolidayService.ProRate(10m, daysWorked, daysInMonth);

        Assert.Equal((decimal)expected, result);
    }
}