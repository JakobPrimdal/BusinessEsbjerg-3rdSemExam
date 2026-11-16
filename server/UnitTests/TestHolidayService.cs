using Infrastructure.Entities;
using Service;

namespace UnitTests;

    // Unit tests for the holiday rules in HolidayService
public class TestHolidayService
{
    // How many hours of holiday (weeks 1-5) you earn per month 
    // Rule: 2.08 days per month

    [Fact]
    public void CalculateWeeks1To5Hours_FullTime37Hours_Returns15Point392()
    {
        // Arrange
        decimal weeklyHours = 37m;
        decimal daysPerMonth = 2.08m;

        // Act
        decimal result = HolidayService.CalculateWeeks1To5Hours(weeklyHours, daysPerMonth);

        // Assert: 2.08 * 37 / 5 = 15.392
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
}