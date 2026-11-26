using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Service;
using Service.DTOs;

namespace Api.Controllers;

[Authorize]
public class HolidayController(
    HolidayService holidayService,
    HolidayRequestService requestService,
    HolidayDecisionService decisionService,
    ClosedDayService closedDayService) : ControllerBase
{
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

    [HttpPost("holiday/" + nameof(PreviewRequest))]
    public HolidayRequestPreviewDTO PreviewRequest([FromBody] CreateHolidayRequestDTO dto)
        => requestService.Preview(CurrentUserId(), dto);

    [HttpPost("holiday/" + nameof(RequestHoliday))]
    public HolidayRequestDTO RequestHoliday([FromBody] CreateHolidayRequestDTO dto)
        => requestService.Create(CurrentUserId(), dto);
    
    [Authorize(Roles = "Accountant")]
    [HttpGet("holiday/" + nameof(GetPendingRequests))]
    public List<PendingHolidayRequestDTO> GetPendingRequests()
        => decisionService.GetPending();
    
    [Authorize(Roles = "Accountant")]
    [HttpPost("holiday/" + nameof(ApproveRequest) + "/{requestId:guid}")]
    public HolidayDecisionDTO ApproveRequest(Guid requestId)
        => decisionService.Approve(requestId, CurrentUserId());
    
    [Authorize(Roles = "Accountant")]
    [HttpPost("holiday/" + nameof(DenyRequest) + "/{requestId:guid}")]
    public HolidayDecisionDTO DenyRequest(Guid requestId, [FromBody] DenyHolidayRequestDTO dto)
        => decisionService.Deny(requestId, CurrentUserId(), dto.Reason);

    
    // Closed days
    
    [HttpGet("holiday/" + nameof(GetClosedDays))]
    public ActionResult<ClosedDayResponseDTO> GetClosedDays()
    {
        return Ok(closedDayService.GetAllClosedDays());
    }

    [HttpPost("holiday/" + nameof(CreateClosedDay))]
    public ActionResult<ClosedDayResponseDTO> CreateClosedDay([FromBody] ClosedDayRequestDTO dto)
    {
        return closedDayService.CreateClosedDay(dto);
    }

    [HttpPost("holiday/" + nameof(UpdateClosedDay))]
    public ActionResult<ClosedDayResponseDTO> UpdateClosedDay(Guid id, [FromBody] ClosedDayRequestDTO dto)
    {
        return closedDayService.UpdateClosedDay(id, dto);
    }

    [HttpPost("holiday/" + nameof(ToggleDelete))]
    public ActionResult<ClosedDayResponseDTO> ToggleDelete(Guid id)
    {
        return closedDayService.ToggleIsDeleted(id);
    }
    
    // Helpers
    private string CurrentUserId()
        => HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
}