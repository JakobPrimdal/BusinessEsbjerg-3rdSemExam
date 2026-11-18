using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Service;
using Service.DTOs;

namespace Api.Controllers;

public class HolidayController(HolidayService holidayService, HolidayRequestService requestService) : ControllerBase
{
    [Authorize]
    [HttpGet("holiday/" + nameof(GetMyBalance))]
    public HolidayBalanceDTO GetMyBalance()
        => holidayService.GetBalance(CurrentUserId());

    [Authorize(Roles = "Admin,Accountant")]
    [HttpGet("holiday/" + nameof(GetBalance) + "/{userId}")]
    public HolidayBalanceDTO GetBalance(string userId)
        => holidayService.GetBalance(userId);

    [Authorize(Roles = "Admin")]
    [HttpPost("holiday/" + nameof(AddWorkHours))]
    public IActionResult AddWorkHours([FromBody] CreateWorkHourDTO dto)
    {
        var row = holidayService.AddWorkHours(dto);
        return Ok(new { row.Id, row.UserId, row.ValidFrom });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("holiday/" + nameof(AddMonth))]
    public IActionResult AddMonth([FromQuery] DateOnly month)
    {
        var count = holidayService.AddMonthlyHolidayForAll(month);
        return Ok(new { month = HolidayService.FirstDayOfMonth(month), usersUpdated = count });
    }

   [Authorize]
    [HttpPost("holiday/" + nameof(PreviewRequest))]
    public HolidayRequestPreviewDTO PreviewRequest([FromBody] CreateHolidayRequestDTO dto)
        => requestService.Preview(CurrentUserId(), dto);

    [Authorize]
    [HttpPost("holiday/" + nameof(RequestHoliday))]
    public HolidayRequestDTO RequestHoliday([FromBody] CreateHolidayRequestDTO dto)
        => requestService.Create(CurrentUserId(), dto);
    
    private string CurrentUserId()
        => HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
}