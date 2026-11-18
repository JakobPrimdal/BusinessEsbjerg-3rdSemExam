using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.Exceptions;
using Service.Interfaces;

namespace Service;

public class HolidayRuleSetService(HolidayDb db, TimeProvider clock) : IHolidayRuleSet
{
    public HolidayRuleSet? GetRulesForDate(DateOnly date)
    {
        return db.HolidayRuleSets
            .Where(r => r.ValidFrom <= date)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();
    }

    public HolidayRuleSet? GetCurrentRules()
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

        return GetRulesForDate(today);
    }

    public IReadOnlyList<HolidayRuleSet> GetAllRules()
    {
        return db.HolidayRuleSets
            .OrderByDescending(r => r.ValidFrom)
            .ToList();
    }

    public HolidayRuleSet CreateVersion(HolidayRuleSet ruleSet, string createdBy)
    {
        if (ruleSet is null)
            throw new BadRequestException("Rule set is required.");

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new BadRequestException("CreatedBy is required.");

        ValidateRuleSet(ruleSet);

        // Get the latest existing version.
        var latestVersion = db.HolidayRuleSets
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefault();

        // New versions must be chronologically ordered.
        if (latestVersion != null &&
            ruleSet.ValidFrom <= latestVersion.ValidFrom)
        {
            throw new BadRequestException("ValidFrom must be later than the latest version: " + latestVersion.ValidFrom + ".");
        }

        // Never reuse metadata supplied by the caller.
        ruleSet.Id = Guid.NewGuid();
        ruleSet.CreatedBy = createdBy;
        ruleSet.CreatedAt = clock.GetUtcNow().UtcDateTime;

        db.Insert(ruleSet);

        return ruleSet;
    }

    private static void ValidateRuleSet(HolidayRuleSet ruleSet)
    {
        // Earning rates
        if (ruleSet.Weeks1To5EarningRate < 0)
            throw new BadRequestException("Weeks 1-5 earning rate cannot be negative.");

        if (ruleSet.Week6EarningRate < 0)
            throw new BadRequestException("Week 6 earning rate cannot be negative.");

        // Earning periods
        ValidatePeriod(
            ruleSet.Weeks1To5EarningStartMonth,
            ruleSet.Weeks1To5EarningStartDay,
            0,
            ruleSet.Weeks1To5EarningEndMonth,
            ruleSet.Weeks1To5EarningEndDay,
            ruleSet.Weeks1To5EarningEndYearOffset,
            "Weeks 1-5 earning period");

        ValidatePeriod(
            ruleSet.Week6EarningStartMonth,
            ruleSet.Week6EarningStartDay,
            0,
            ruleSet.Week6EarningEndMonth,
            ruleSet.Week6EarningEndDay,
            ruleSet.Week6EarningEndYearOffset,
            "Week 6 earning period");

        // Usage periods
        ValidatePeriod(
            ruleSet.Weeks1To5UsageStartMonth,
            ruleSet.Weeks1To5UsageStartDay,
            ruleSet.Weeks1To5UsageStartYearOffset,
            ruleSet.Weeks1To5UsageEndMonth,
            ruleSet.Weeks1To5UsageEndDay,
            ruleSet.Weeks1To5UsageEndYearOffset,
            "Weeks 1-5 usage period");

        ValidatePeriod(
            ruleSet.Week6UsageStartMonth,
            ruleSet.Week6UsageStartDay,
            ruleSet.Week6UsageStartYearOffset,
            ruleSet.Week6UsageEndMonth,
            ruleSet.Week6UsageEndDay,
            ruleSet.Week6UsageEndYearOffset,
            "Week 6 usage period");

        // Main holiday period
        ValidateDate(
            ruleSet.MainHolidayStartMonth,
            ruleSet.MainHolidayStartDay,
            0,
            "Main holiday start");

        ValidateDate(
            ruleSet.MainHolidayEndMonth,
            ruleSet.MainHolidayEndDay,
            0,
            "Main holiday end");

        // Transfer rules
        if (ruleSet.MaximumTransferDays < 0 || ruleSet.MaximumTransferDays > 5)
            throw new BadRequestException("Maximum transfer days must be between 0 and 5.");

        ValidateDate(
            ruleSet.Weeks1To5TransferDeadlineMonth,
            ruleSet.Weeks1To5TransferDeadlineDay,
            ruleSet.Weeks1To5TransferDeadlineYearOffset,
            "Weeks 1-5 transfer deadline");

        ValidateDate(
            ruleSet.Week6TransferDeadlineMonth,
            ruleSet.Week6TransferDeadlineDay,
            ruleSet.Week6TransferDeadlineYearOffset,
            "Week 6 transfer deadline");

        // Other rules
        if (ruleSet.SickWaitingDays < 0)
            throw new BadRequestException("Sick waiting days cannot be negative.");

        if (ruleSet.MainHolidayNoticePeriodMonths < 0)
            throw new BadRequestException("Employee notice period cannot be negative.");

        if (ruleSet.OtherHolidayNoticePeriodMonths < 0)
            throw new BadRequestException("Employer notice period cannot be negative.");
    }

    private static void ValidatePeriod(
        int startMonth,
        int startDay,
        int startYearOffset,
        int endMonth,
        int endDay,
        int endYearOffset,
        string name)
    {
        ValidateDate(startMonth, startDay, startYearOffset, name);
        ValidateDate(endMonth, endDay, endYearOffset, name);

        var start = new DateOnly(
            2000 + startYearOffset,
            startMonth,
            startDay);

        var end = new DateOnly(
            2000 + endYearOffset,
            endMonth,
            endDay);

        if (start > end)
            throw new BadRequestException(name + ": start date must be before or equal to end date.");
    }

    private static void ValidateDate(
        int month,
        int day,
        int yearOffset,
        string name)
    {
        if (yearOffset < 0 || yearOffset > 100)
            throw new BadRequestException(name + ": year offset must be between 0 and 100.");

        if (month < 1 || month > 12)
            throw new BadRequestException(name + ": month must be between 1 and 12.");

        var year = 2000 + yearOffset;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        if (day < 1 || day > daysInMonth)
            throw new BadRequestException(name + ": invalid day for the selected month.");
    }
}
