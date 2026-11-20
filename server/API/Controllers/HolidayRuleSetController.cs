using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;

namespace Api.Controllers;

public class HolidayRuleSetController(IHolidayRuleSetService ruleSetService) : ControllerBase
{
    [HttpGet(nameof(GetHolidayRuleSets))]
    public ActionResult<IReadOnlyList<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO>> GetHolidayRuleSets()
    {
        return Ok(ruleSetService.GetAllRuleSets());
    }

    [HttpGet(nameof(GetCurrent))]
    public ActionResult<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> GetCurrent()
    {
        return ruleSetService.GetCurrentRuleSet()!;
    }

    [HttpGet(nameof(GetHolidayRuleSetByDate))]
    public ActionResult<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> GetHolidayRuleSetByDate(DateOnly date)
    {
        return ruleSetService.GetRulesForDate(date)!;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost(nameof(CreateHolidayRuleSet))]
    public ActionResult<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> CreateHolidayRuleSet(HolidayRuleSetDTOs.HolidayRuleSetRequestDTO dto)
    {
        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;

        return ruleSetService.CreateVersion(dto, createdBy);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost(nameof(ToggleActive))]
    public ActionResult<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> ToggleActive(Guid id)
    {
        return ruleSetService.ToggleActive(id);
    }

    [HttpGet(nameof(GetRulesById))]
    public ActionResult<HolidayRuleSetDTOs.HolidayRuleSetResponseDTO> GetRulesById(Guid id)
    {
        return ruleSetService.GetRulesById(id);
    }
}