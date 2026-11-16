using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Service;
using Service.DTOs;
using Service.Exceptions;

namespace IntegrationTests;

// The unit tests check the formulas on their own. These tests check that the service
// reads work hours and rules from the database, calculates correctly and saves the result.
public class TestHolidayServiceWithDatabase : IDisposable
{
    private readonly string _dbPath;
    private readonly HolidayDb _db;
    private readonly HolidayService _sut;

    // A fake clock so now is always the same in the tests
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public TestHolidayServiceWithDatabase()
    {
        // Arrange - a new database with the tables and the two holiday rules
        _dbPath = Path.Combine(Path.GetTempPath(), $"holiday-service-{Guid.NewGuid():N}.db");
        var options = new DataOptions<HolidayDb>(new DataOptions().UseSQLite($"Data Source={_dbPath}"));
        _db = new HolidayDb(options);

        _db.CreateTable<User>();
        _db.CreateTable<RuleValue>();
        _db.CreateTable<WorkHour>();
        _db.CreateTable<HolidayAccount>();
        _db.CreateTable<HolidayTransaction>();
        
        AddRule(HolidayService.DaysPerMonthKey, "2.08", new DateOnly(2020, 9, 1));
        AddRule(HolidayService.Week6HoursPerMonthKey, "3.083", new DateOnly(2020, 9, 1));

        _sut = new HolidayService(_db, new FixedClock(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools(); // release the file so it can be deleted
        File.Delete(_dbPath);
    }

    //  helpers 

    private void AddRule(string key, string value, DateOnly validFrom)
    {
        _db.Insert(new RuleValue
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            ValidFrom = validFrom,
            CreatedAt = DateTime.UtcNow
        });
    }

    // Puts a user straight into the database and returns the id
    private string CreateUser()
    {
        var userId = Guid.NewGuid().ToString();
        _db.Insert(new User
        {
            UserId = userId,
            Username = "user" + userId[..8],
            PasswordHash = "not-used-here",
            Role = UserRole.User
        });
        return userId;
    }

    // Same hours every weekday, nothing in the weekend
    private void AddWorkWeek(string userId, DateOnly validFrom, decimal hoursPerDay)
    {
        _sut.AddWorkHours(new CreateWorkHourDTO(userId, validFrom,
            hoursPerDay, hoursPerDay, hoursPerDay, hoursPerDay, hoursPerDay, 0m, 0m));
    }

    //  AddWorkHours: saving + server-side validation 

    [Fact]
    public void AddWorkHours_ValidWeek_IsSavedInDatabase()
    {
        // Arrange
        var userId = CreateUser();

        // Act
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);

        // Assert: read it back from the database
        var saved = Assert.Single(_db.WorkHours.Where(w => w.UserId == userId).ToList());
        Assert.Equal(37m, HolidayService.WeeklyHours(saved));
    }

    [Theory]
    [InlineData(-1)] // negative hours
    [InlineData(25)] // more hours than a day has
    public void AddWorkHours_InvalidHoursPerDay_ThrowsAndSavesNothing(int mondayHours)
    {
        // Arrange
        var userId = CreateUser();
        var dto = new CreateWorkHourDTO(userId, new DateOnly(2026, 1, 1), mondayHours, 7.4m, 7.4m, 7.4m, 7.4m, 0m, 0m);

        // Act + Assert
        Assert.Throws<BadRequestException>(() => _sut.AddWorkHours(dto));
        Assert.False(_db.WorkHours.Any(w => w.UserId == userId));
    }

    [Fact]
    public void AddWorkHours_UnknownUser_ThrowsNotFound()
    {
        var dto = new CreateWorkHourDTO(Guid.NewGuid().ToString(), new DateOnly(2026, 1, 1), 7.4m, 7.4m, 7.4m, 7.4m, 7.4m, 0m, 0m);

        Assert.Throws<NotFoundException>(() => _sut.AddWorkHours(dto));
    }

    // ---------- AddMonthlyHoliday: the calculation with data from the database ----------

    [Fact]
    public void AddMonthlyHoliday_FullTimeEmployee_SavesCorrectHours()
    {
        // Arrange: 37 hours a week from January
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);

        // Act
        bool added = _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        var balance = _sut.GetBalance(userId);

        // Assert
        Assert.True(added);
        Assert.Equal(15.392m, balance.Weeks1To5Hours); // 2.08 * 37 / 5
        Assert.Equal(3.083m, balance.Week6Hours);      // full time gets the full rule value
    }

    [Fact]
    public void AddMonthlyHoliday_PartTimeEmployee_GetsProportionalHours()
    {
        // Arrange: 18.5 hours a week (half time)
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 3.7m);

        // Act
        _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        var balance = _sut.GetBalance(userId);

        // Assert: exactly half of a full-time employee
        Assert.Equal(7.696m, balance.Weeks1To5Hours); // 2.08 * 18.5 / 5
        Assert.Equal(1.5415m, balance.Week6Hours);    // 3.083 / 2
    }

    [Fact]
    public void AddMonthlyHoliday_WorkHoursChanged_UsesNewestSchedule()
    {
        // Arrange: full time from January, then half time from March
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);
        AddWorkWeek(userId, new DateOnly(2026, 3, 1), 3.7m);

        // Act: holiday for March
        _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        var balance = _sut.GetBalance(userId);

        // Assert: March is calculated with the new 18.5 hour week
        Assert.Equal(7.696m, balance.Weeks1To5Hours);
    }

    [Fact]
    public void AddMonthlyHoliday_RuleChanged_UsesNewestRule()
    {
        // Arrange: from 2027 the rule changes to 2.5 days per month
        AddRule(HolidayService.DaysPerMonthKey, "2.5", new DateOnly(2027, 1, 1));
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);

        // Act
        _sut.AddMonthlyHoliday(userId, new DateOnly(2027, 1, 1));
        var balance = _sut.GetBalance(userId);

        // Assert
        Assert.Equal(18.5m, balance.Weeks1To5Hours); // 2.5 * 37 / 5
    }

    [Fact]
    public void AddMonthlyHoliday_SameMonthTwice_OnlyAddsOnce()
    {
        // Arrange
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);

        // Act: e.g. the server restarts and the monthly job runs again
        bool first = _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        bool second = _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        var balance = _sut.GetBalance(userId);

        // Assert
        Assert.True(first);
        Assert.False(second);
        Assert.Equal(15.392m, balance.Weeks1To5Hours);
    }

    [Fact]
    public void AddMonthlyHoliday_TwoMonths_AddsUp()
    {
        // Arrange
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 1, 1), 7.4m);

        // Act
        _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 3, 1));
        _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 4, 1));
        var balance = _sut.GetBalance(userId);

        // Assert
        Assert.Equal(30.784m, balance.Weeks1To5Hours); // 2 * 15.392
        Assert.Equal(6.166m, balance.Week6Hours);      // 2 * 3.083
    }

    [Fact]
    public void AddMonthlyHoliday_BeforeEmployeeStarted_AddsNothing()
    {
        // Arrange: starts working in March
        var userId = CreateUser();
        AddWorkWeek(userId, new DateOnly(2026, 3, 1), 7.4m);

        // Act: holiday for January
        bool added = _sut.AddMonthlyHoliday(userId, new DateOnly(2026, 1, 1));
        var balance = _sut.GetBalance(userId);

        // Assert
        Assert.False(added);
        Assert.Equal(0m, balance.Weeks1To5Hours);
        Assert.Empty(balance.Accounts);
    }
    
    [Fact]
    public void AddMonthlyHolidayForAll_OnlyUsersWithWorkHoursGetHoliday()
    {
        // Arrange: two employees with work hours, one without
        var anna = CreateUser();
        var bo = CreateUser();
        var noHours = CreateUser();
        AddWorkWeek(anna, new DateOnly(2026, 1, 1), 7.4m);
        AddWorkWeek(bo, new DateOnly(2026, 1, 1), 3.7m);

        // Act
        int count = _sut.AddMonthlyHolidayForAll(new DateOnly(2026, 3, 1));

        // Assert
        Assert.Equal(2, count);
        Assert.Equal(15.392m, _sut.GetBalance(anna).Weeks1To5Hours);
        Assert.Equal(7.696m, _sut.GetBalance(bo).Weeks1To5Hours);
        Assert.Equal(0m, _sut.GetBalance(noHours).Weeks1To5Hours);
    }

    [Fact]
    public void GetBalance_UnknownUser_ThrowsNotFound()
    {
        Assert.Throws<NotFoundException>(() => _sut.GetBalance(Guid.NewGuid().ToString()));
    }
}