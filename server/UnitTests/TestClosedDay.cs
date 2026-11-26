using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Entities.Enums;
using Infrastructure.Seed;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Service;
using Service.DTOs;
using Service.Exceptions;

namespace UnitTests;

public class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public HolidayDb Db { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DataOptions()
            .UseSQLite()
            .UseConnectionFactory(_ => _connection);

        Db = new HolidayDb(new DataOptions<HolidayDb>(options));
        Db.CreateTable<ClosedDay>();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

// Service tests

public class ClosedDayServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly ClosedDayService _service;

    public ClosedDayServiceTests() => _service = new ClosedDayService(_testDb.Db);

    public void Dispose() => _testDb.Dispose();

    private readonly ClosedDayRequestDTO _valid = new()
    {
        Name = "Firma udflugt",
        Type = ClosedDayType.CompanyHoliday,
        Date = new DateOnly(2026, 6, 12),
        IsRecurring = false,
        RecurringUntil = null
    };

    [Fact]
    public void Create_ValidRequest_IsPersistedAndReturned()
    {
        var result = _service.CreateClosedDay(_valid with { Name = "  Firma udflugt  " });

        Assert.Equal("Firma udflugt", result.Name); // trimmed
        Assert.Single(_service.GetAllClosedDays());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
    {
        Assert.Throws<BadRequestException>(() => _service.CreateClosedDay(_valid with { Name = name }));
    }

    [Fact]
    public void Create_RecurringUntilBeforeDate_Throws()
    {
        var dto = _valid with { IsRecurring = true, RecurringUntil = new DateOnly(2026, 1, 1) };

        Assert.Throws<BadRequestException>(() => _service.CreateClosedDay(dto));
    }

    [Fact]
    public void Create_RecurringUntilWithoutIsRecurring_Throws()
    {
        var dto = _valid with { IsRecurring = false, RecurringUntil = new DateOnly(2030, 1, 1) };

        Assert.Throws<BadRequestException>(() => _service.CreateClosedDay(dto));
    }

    [Fact]
    public void Create_SameDateAndType_Throws()
    {
        _service.CreateClosedDay(_valid);

        Assert.Throws<BadRequestException>(() => _service.CreateClosedDay(_valid));
    }

    [Fact]
    public void Update_WithoutChangingDateOrType_DoesNotConflictWithItself()
    {
        var created = _service.CreateClosedDay(_valid);

        var updated = _service.UpdateClosedDay(created.Id, _valid with { Name = "Nyt navn" });

        Assert.Equal("Nyt navn", updated.Name);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public void Update_UnknownId_ThrowsNotFound()
    {
        Assert.Throws<NotFoundException>(() => _service.UpdateClosedDay(Guid.NewGuid(), _valid));
    }

    [Fact]
    public void Delete_DoesNotHideRowFromGetAll()
    {
        var created = _service.CreateClosedDay(_valid);

        _service.ToggleIsDeleted(created.Id);

        var all = _service.GetAllClosedDays();
        var row = Assert.Single(all);
        Assert.True(row.IsDeleted);
    }
}

// Seeder tests

public class ClosedDaySeederTests
{
    [Fact]
    public void Seed_CalculatesMovingHolidaysFromEaster()
    {
        using var testDb = new TestDb();
        var db = testDb.Db;

        ClosedDaySeeder.Seed(db);

        var dates = db.ClosedDays
            .Where(x => x.Type == ClosedDayType.PublicHoliday)
            .ToList()
            .ToDictionary(x => x.Name + x.Date.Year, x => x.Date);

        Assert.Equal(new DateOnly(2026, 4, 3), dates["Langfredag2026"]);
        Assert.Equal(new DateOnly(2026, 4, 6), dates["2. påskedag2026"]);
        Assert.Equal(new DateOnly(2026, 5, 14), dates["Kristi himmelfartsdag2026"]);
        Assert.Equal(new DateOnly(2026, 5, 25), dates["2. pinsedag2026"]);
    }

    [Fact]
    public void Seed_RunTwice_DoesNotCreateDuplicates()
    {
        using var testDb = new TestDb();
        var db = testDb.Db;

        ClosedDaySeeder.Seed(db);
        var countAfterFirst = db.ClosedDays.Count();
        ClosedDaySeeder.Seed(db);

        Assert.Equal(countAfterFirst, db.ClosedDays.Count());
    }
}