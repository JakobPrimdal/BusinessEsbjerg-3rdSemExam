using Infrastructure.Entities;
using Service.DTOs;

namespace Service.Interfaces;

public interface IHolidayRuleSetService
{
    IReadOnlyList<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> GetAllRuleSets();
    
    HolidayRuleSetDTOs.HolidayRuleSetResponseDTO? GetRulesForDate(DateOnly date);

    HolidayRuleSetDTOs.HolidayRuleSetResponseDTO? GetCurrentRuleSet();

    HolidayRuleSetDTOs.HolidayRuleSetResponseDTO CreateVersion(HolidayRuleSetDTOs.HolidayRuleSetRequestDTO ruleSet, string createdBy);
}