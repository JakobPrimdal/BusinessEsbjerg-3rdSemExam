using Infrastructure.Entities;
using LinqToDB;

namespace Infrastructure.Seed;

public static class HolidayRuleSetSeeder
{
    public static void Seed(HolidayDb db, TimeProvider clock)
    {
        // Do not overwrite existing rule sets.
        if (db.HolidayRuleSets.Any())
            return;

        HolidayRuleSet initialRuleSet = new HolidayRuleSet
        {
            Id = Guid.NewGuid(),
            ValidFrom = new DateOnly(2020, 9, 1),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            CreatedBy = null,

            // Earning rates
            Weeks1To5EarningRate = 2.08m,
            Week6EarningRate = 3.083m,

            // Weeks 1-5 earning period:
            // September 1st - August 31st next year
            Weeks1To5EarningStartDay = 1,
            Weeks1To5EarningStartMonth = 9,
            Weeks1To5EarningEndDay = 31,
            Weeks1To5EarningEndMonth = 8,
            Weeks1To5EarningEndYearOffset = 1,

            // Week 6 earning period:
            // January 1st - December 31st
            Week6EarningStartDay = 1,
            Week6EarningStartMonth = 1,
            Week6EarningEndDay = 31,
            Week6EarningEndMonth = 12,
            Week6EarningEndYearOffset = 0,

            // Weeks 1-5 usage period:
            // September 1st - December 31st next year
            Weeks1To5UsageStartDay = 1,
            Weeks1To5UsageStartMonth = 9,
            Weeks1To5UsageStartYearOffset = 0,
            Weeks1To5UsageEndDay = 31,
            Weeks1To5UsageEndMonth = 12,
            Weeks1To5UsageEndYearOffset = 1,

            // Week 6 usage period:
            // May 1st next year - April 30th two years later
            Week6UsageStartDay = 1,
            Week6UsageStartMonth = 5,
            Week6UsageStartYearOffset = 1,
            Week6UsageEndDay = 30,
            Week6UsageEndMonth = 4,
            Week6UsageEndYearOffset = 2,

            // Main holiday period
            MainHolidayStartDay = 1,
            MainHolidayStartMonth = 5,
            MainHolidayEndDay = 30,
            MainHolidayEndMonth = 9,

            // Holiday transfer
            HolidayTransferEnabled = false,
            MaximumTransferDays = 5,

            Weeks1To5TransferDeadlineDay = 31,
            Weeks1To5TransferDeadlineMonth = 12,
            Weeks1To5TransferDeadlineYearOffset = 1,

            Week6TransferDeadlineDay = 30,
            Week6TransferDeadlineMonth = 9,
            Week6TransferDeadlineYearOffset = 2,

            // Other rules
            SickWaitingDays = 0,
            MainHolidayNoticePeriodMonths = 3,
            OtherHolidayNoticePeriodMonths = 1
        };

        db.Insert(initialRuleSet);
    }
}
