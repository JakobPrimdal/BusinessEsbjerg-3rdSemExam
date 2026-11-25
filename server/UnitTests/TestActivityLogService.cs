using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Service;
using Service.Exceptions;

namespace UnitTests.Service;

public class TestActivityLogService : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HolidayDb _db;
    private readonly ActivityLogService _sut;
    private readonly MovableClock _clock = new(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

    public TestActivityLogService()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DataOptions()
            .UseSQLite()
            .UseConnectionFactory(_ => _connection);

        _db = new HolidayDb(new DataOptions<HolidayDb>(options));
        _db.CreateTable<User>();
        _db.CreateTable<ActivityLog>();

        _sut = new ActivityLogService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // Log

    [Fact]
    public void Log_StoresUserActionDetailsAndTime()
    {
        var anna = CreateUser("anna");

        _sut.Log(anna, ActivityLogActions.HolidayRequested, "Anmodede om ferie");

        var row = Assert.Single(_db.ActivityLogs.ToList());
        Assert.Equal(anna, row.UserId);
        Assert.Equal("anna", row.Username);
        Assert.Equal(ActivityLogActions.HolidayRequested, row.Action);
        Assert.Equal("Anmodede om ferie", row.Details);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, row.CreatedAt);
    }

    [Fact]
    public void Log_WithoutUser_IsLoggedAsSystem()
    {
        _sut.Log(null, ActivityLogActions.MonthlyHolidayAdded, "Månedlig ferie");

        var row = Assert.Single(_db.ActivityLogs.ToList());
        Assert.Null(row.UserId);
        Assert.Equal(ActivityLogService.SystemUsername, row.Username);
    }

    [Fact]
    public void Log_KeepsTheUsernameFromTheTimeOfTheAction()
    {
        var anna = CreateUser("anna");
        _sut.Log(anna, ActivityLogActions.HolidayRequested, "x");

        // the user is renamed later
        _db.Users.Where(u => u.UserId == anna).Set(u => u.Username, "anna.b").Update();

        Assert.Equal("anna", _db.ActivityLogs.Single().Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Log_WithoutAction_Throws(string action)
    {
        Assert.Throws<ArgumentException>(() => _sut.Log(null, action, "x"));
    }

    // GetLogs

    [Fact]
    public void GetLogs_ReturnsNewestFirst_WithUtcTimes()
    {
        LogAt(new DateTime(2026, 11, 1, 8, 0, 0), null, "first");
        LogAt(new DateTime(2026, 11, 3, 8, 0, 0), null, "third");
        LogAt(new DateTime(2026, 11, 2, 8, 0, 0), null, "second");

        var result = _sut.GetLogs(null, null, null, page: 1);

        Assert.Equal(["third", "second", "first"], result.Items.Select(i => i.Details));
        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal(DateTimeKind.Utc, i.CreatedAt.Kind));
    }

    [Fact]
    public void GetLogs_FilterByUser_OnlyReturnsThatUsersActions()
    {
        var anna = CreateUser("anna");
        var bo = CreateUser("bo");
        _sut.Log(anna, ActivityLogActions.HolidayRequested, "anna 1");
        _sut.Log(bo, ActivityLogActions.HolidayRequested, "bo 1");
        _sut.Log(anna, ActivityLogActions.HolidayRequested, "anna 2");

        var result = _sut.GetLogs(anna, null, null, page: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal(anna, i.UserId));
    }

    [Fact]
    public void GetLogs_FilterByDate_FromIsInclusive_ToIsExclusive()
    {
        LogAt(new DateTime(2026, 11, 9, 23, 59, 59), null, "before");
        LogAt(new DateTime(2026, 11, 10, 0, 0, 0), null, "on from");
        LogAt(new DateTime(2026, 11, 10, 23, 59, 59), null, "last second");
        LogAt(new DateTime(2026, 11, 11, 0, 0, 0), null, "on to");

        var from = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
        var result = _sut.GetLogs(null, from, to, page: 1);

        Assert.Equal(["last second", "on from"], result.Items.Select(i => i.Details));
    }

    [Fact]
    public void GetLogs_DateFilter_UsesTheOffsetOfTheGivenTime()
    {
        // 00:30 Danish time on 10 Nov is 23:30 UTC on 9 Nov
        LogAt(new DateTime(2026, 11, 9, 23, 30, 0), null, "just after Danish midnight");

        var danishMidnight = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.FromHours(1));
        var result = _sut.GetLogs(null, danishMidnight, null, page: 1);

        Assert.Single(result.Items);
    }

    [Fact]
    public void GetLogs_FromAfterTo_ThrowsBadRequest()
    {
        var from = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

        var ex = Assert.Throws<BadRequestException>(() => _sut.GetLogs(null, from, to, page: 1));
        Assert.Equal(ActivityLogService.InvalidDateRangeError, ex.Message);
    }

    [Fact]
    public void GetLogs_SplitsIntoPages()
    {
        var start = new DateTime(2026, 1, 1);
        for (var i = 0; i < ActivityLogService.PageSize + 5; i++)
            LogAt(start.AddMinutes(i), null, $"row {i}");

        var page1 = _sut.GetLogs(null, null, null, page: 1);
        var page2 = _sut.GetLogs(null, null, null, page: 2);

        Assert.Equal(ActivityLogService.PageSize + 5, page1.TotalCount);
        Assert.Equal(ActivityLogService.PageSize, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal("row 4", page2.Items.First().Details);   // page 2 continues where page 1 stopped
        Assert.Equal("row 0", page2.Items.Last().Details);    // oldest overall is last on the last page
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public void GetLogs_PageBelowOne_ReturnsFirstPage()
    {
        _sut.Log(null, ActivityLogActions.MonthlyHolidayAdded, "x");

        var result = _sut.GetLogs(null, null, null, page: 0);

        Assert.Equal(1, result.Page);
        Assert.Single(result.Items);
    }

    // GetUsers

    [Fact]
    public void GetUsers_ReturnsAllUsersSortedByUsername()
    {
        CreateUser("bo");
        CreateUser("anna");

        var users = _sut.GetUsers();

        Assert.Equal(["anna", "bo"], users.Select(u => u.Username));
    }

    // helpers

    private string CreateUser(string username)
    {
        var id = Guid.NewGuid().ToString();
        _db.Insert(new User { UserId = id, Username = username, PasswordHash = "x", Role = UserRole.User });
        return id;
    }

    private void LogAt(DateTime utc, string? userId, string details)
    {
        _clock.Now = new DateTimeOffset(utc, TimeSpan.Zero);
        _sut.Log(userId, ActivityLogActions.HolidayRequested, details);
    }

    private sealed class MovableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}