using Infrastructure.Entities;

namespace Service.Interfaces;

public interface IHolidayRuleSet
{
    HolidayRuleSet? GetRulesForDate(DateOnly date);

    HolidayRuleSet? GetCurrentRules();

    IReadOnlyList<HolidayRuleSet> GetAllRules();

    HolidayRuleSet CreateVersion(HolidayRuleSet ruleSet, string createdBy);
}