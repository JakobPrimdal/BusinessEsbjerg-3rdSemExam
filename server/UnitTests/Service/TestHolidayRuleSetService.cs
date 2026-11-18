using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Service;
using Service.Exceptions;

namespace UnitTests.Service;

public class TestHolidayRuleSetService : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly HolidayDb _db;
    private readonly HolidayRuleSetService _sut;
    private readonly FixedTimeProvider _clock;

    private static readonly DateTimeOffset FixedNow = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    public TestHolidayRuleSetService()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DataOptions()
            .UseSQLite()
            .UseConnectionFactory(_ => _connection);

        _db = new HolidayDb(new DataOptions<HolidayDb>(options));
        _db.CreateTable<HolidayRuleSet>();

        _clock = new FixedTimeProvider(FixedNow);
        _sut = new HolidayRuleSetService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // GetRulesForDate tests

    [Fact]
    public void GetRulesForDate_ReturnsLatestApplicableVersion()
    {
        var oldVersion = ValidRuleSet(new DateOnly(2020, 9, 1));
        var newVersion = ValidRuleSet(new DateOnly(2025, 9, 1));

        _db.Insert(oldVersion);
        _db.Insert(newVersion);

        var result = _sut.GetRulesForDate(new DateOnly(2026, 1, 1));

        Assert.NotNull(result);
        Assert.Equal(newVersion.Id, result.Id);
    }

    [Fact]
    public void GetRulesForDate_ReturnsNull_WhenNoVersionExists()
    {
        _db.Insert(ValidRuleSet(new DateOnly(2025, 9, 1)));

        var result = _sut.GetRulesForDate(new DateOnly(2025, 8, 31));

        Assert.Null(result);
    }

    // GetCurrentRules test

    [Fact]
    public void GetCurrentRules_ReturnsRulesValidToday()
    {
        var oldVersion = ValidRuleSet(new DateOnly(2020, 9, 1));
        var currentVersion = ValidRuleSet(new DateOnly(2026, 9, 1));
        var futureVersion = ValidRuleSet(new DateOnly(2027, 9, 1));

        _db.Insert(oldVersion);
        _db.Insert(currentVersion);
        _db.Insert(futureVersion);

        var result = _sut.GetCurrentRules();

        Assert.NotNull(result);
        Assert.Equal(currentVersion.Id, result.Id);
    }

    // GetAllRules test

    [Fact]
    public void GetAllRules_ReturnsRulesOrderedByValidFromDescending()
    {
        var oldest = ValidRuleSet(new DateOnly(2020, 9, 1));
        var newest = ValidRuleSet(new DateOnly(2026, 9, 1));
        var middle = ValidRuleSet(new DateOnly(2023, 9, 1));

        _db.Insert(oldest);
        _db.Insert(newest);
        _db.Insert(middle);

        var result = _sut.GetAllRules();

        Assert.Equal(3, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(middle.Id, result[1].Id);
        Assert.Equal(oldest.Id, result[2].Id);
    }

    // CreateVersion tests

    [Fact]
    public void CreateVersion_SetsMetadataAndInsertsRuleSet()
    {
        var ruleSet = ValidRuleSet(new DateOnly(2027, 9, 1));

        // Caller-supplied metadata must be overwritten.
        ruleSet.Id = Guid.Empty;
        ruleSet.CreatedBy = "wrong-user";
        ruleSet.CreatedAt = DateTime.MinValue;

        var result = _sut.CreateVersion(ruleSet, "admin");

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("admin", result.CreatedBy);
        Assert.Equal(FixedNow.UtcDateTime, result.CreatedAt);

        var saved = _db.HolidayRuleSets
            .FirstOrDefault(r => r.Id == result.Id);

        Assert.NotNull(saved);
        Assert.Equal(result.Id, saved.Id);
    }

    [Fact]
    public void CreateVersion_ThrowsWhenValidFromIsNotLater()
    {
        _db.Insert(ValidRuleSet(new DateOnly(2026, 9, 1)));

        var sameDate = ValidRuleSet(new DateOnly(2026, 9, 1));
        var earlierDate = ValidRuleSet(new DateOnly(2025, 9, 1));

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(sameDate, "admin"));

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(earlierDate, "admin"));

        Assert.Equal(1, _db.HolidayRuleSets.Count());
    }

    // Validation tests

    [Fact]
    public void CreateVersion_ThrowsWhenRequiredInputIsMissing()
    {
        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(null!, "admin"));

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(ValidRuleSet(), "  "));
    }

    [Theory]
    [InlineData(-1, 3.083)]
    [InlineData(2.08, -1)]
    public void CreateVersion_ThrowsWhenEarningRateIsNegative(
        decimal weeks1To5Rate,
        decimal week6Rate)
    {
        var ruleSet = ValidRuleSet();
        ruleSet.Weeks1To5EarningRate = weeks1To5Rate;
        ruleSet.Week6EarningRate = week6Rate;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(ruleSet, "admin"));
    }

    [Fact]
    public void CreateVersion_ThrowsWhenPeriodIsInvalid()
    {
        var invalidMonth = ValidRuleSet();
        invalidMonth.Weeks1To5EarningStartMonth = 13;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(invalidMonth, "admin"));

        var invalidOrder = ValidRuleSet();
        invalidOrder.Weeks1To5EarningStartMonth = 10;
        invalidOrder.Weeks1To5EarningStartDay = 1;
        invalidOrder.Weeks1To5EarningEndMonth = 9;
        invalidOrder.Weeks1To5EarningEndDay = 1;
        invalidOrder.Weeks1To5EarningEndYearOffset = 0;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(invalidOrder, "admin"));
    }

    [Fact]
    public void CreateVersion_ThrowsWhenTransferOrOtherRulesAreInvalid()
    {
        var tooManyTransferDays = ValidRuleSet();
        tooManyTransferDays.MaximumTransferDays = 6;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(tooManyTransferDays, "admin"));

        var negativeSickWaitingDays = ValidRuleSet();
        negativeSickWaitingDays.SickWaitingDays = -1;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(negativeSickWaitingDays, "admin"));

        var negativeNoticePeriod = ValidRuleSet();
        negativeNoticePeriod.MainHolidayNoticePeriodMonths = -1;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(negativeNoticePeriod, "admin"));

        var negativeOtherNoticePeriod = ValidRuleSet();
        negativeOtherNoticePeriod.OtherHolidayNoticePeriodMonths = -1;

        Assert.Throws<BadRequestException>(() =>
            _sut.CreateVersion(negativeOtherNoticePeriod, "admin"));
    }

    // Test helpers

    private static HolidayRuleSet ValidRuleSet(
        DateOnly? validFrom = null)
    {
        return new HolidayRuleSet
        {
            Id = Guid.NewGuid(),
            ValidFrom = validFrom ?? new DateOnly(2026, 9, 1),
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,

            Weeks1To5EarningRate = 2.08m,
            Week6EarningRate = 3.083m,

            Weeks1To5EarningStartDay = 1,
            Weeks1To5EarningStartMonth = 9,
            Weeks1To5EarningEndDay = 31,
            Weeks1To5EarningEndMonth = 8,
            Weeks1To5EarningEndYearOffset = 1,

            Week6EarningStartDay = 1,
            Week6EarningStartMonth = 1,
            Week6EarningEndDay = 31,
            Week6EarningEndMonth = 12,
            Week6EarningEndYearOffset = 0,

            Weeks1To5UsageStartDay = 1,
            Weeks1To5UsageStartMonth = 9,
            Weeks1To5UsageStartYearOffset = 0,
            Weeks1To5UsageEndDay = 31,
            Weeks1To5UsageEndMonth = 12,
            Weeks1To5UsageEndYearOffset = 1,

            Week6UsageStartDay = 1,
            Week6UsageStartMonth = 5,
            Week6UsageStartYearOffset = 1,
            Week6UsageEndDay = 30,
            Week6UsageEndMonth = 4,
            Week6UsageEndYearOffset = 2,

            MainHolidayStartDay = 1,
            MainHolidayStartMonth = 5,
            MainHolidayEndDay = 30,
            MainHolidayEndMonth = 9,

            HolidayTransferEnabled = false,
            MaximumTransferDays = 5,

            Weeks1To5TransferDeadlineDay = 31,
            Weeks1To5TransferDeadlineMonth = 12,
            Weeks1To5TransferDeadlineYearOffset = 1,

            Week6TransferDeadlineDay = 30,
            Week6TransferDeadlineMonth = 9,
            Week6TransferDeadlineYearOffset = 2,

            SickWaitingDays = 0,
            MainHolidayNoticePeriodMonths = 3,
            OtherHolidayNoticePeriodMonths = 1
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
