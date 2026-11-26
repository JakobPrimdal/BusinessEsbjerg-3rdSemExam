using System.Security.Claims;
using Infrastructure.Entities; // ADDED
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Service;
using Service.DTOs;

namespace Api.Controllers;

public class HolidayController(
    HolidayService holidayService,
    HolidayRequestService requestService,
    HolidayDecisionService decisionService,
    HolidayCancellationService cancellationService) : ControllerBase 
{
  
    private const string Employee = nameof(UserRole.User);

    [Authorize(Roles = Employee)] 
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

    [Authorize(Roles = Employee)] 
    [HttpPost("holiday/" + nameof(PreviewRequest))]
    public HolidayRequestPreviewDTO PreviewRequest([FromBody] CreateHolidayRequestDTO dto)
        => requestService.Preview(CurrentUserId(), dto);

    [Authorize(Roles = Employee)] 
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
    

    [Authorize(Roles = Employee)]
    [HttpGet("holiday/" + nameof(GetMyRequests))]
    public List<MyHolidayRequestDTO> GetMyRequests()
        => cancellationService.GetMyRequests(CurrentUserId());
    
    [Authorize(Roles = Employee)]
    [HttpPost("holiday/" + nameof(CancelRequest) + "/{requestId:guid}")]
    public HolidayCancellationDTO CancelRequest(Guid requestId)
        => cancellationService.Cancel(CurrentUserId(), requestId);

    [Authorize(Roles = "Accountant")]
    [HttpGet("holiday/" + nameof(GetCancellationRequests))]
    public List<CancellationRequestDTO> GetCancellationRequests()
        => cancellationService.GetCancellationRequests();

    [Authorize(Roles = "Accountant")]
    [HttpPost("holiday/" + nameof(ApproveCancellation) + "/{requestId:guid}")]
    public CancellationDecisionDTO ApproveCancellation(Guid requestId)
        => cancellationService.ApproveCancellation(requestId, CurrentUserId());

    [Authorize(Roles = "Accountant")]
    [HttpPost("holiday/" + nameof(DenyCancellation) + "/{requestId:guid}")]
    public CancellationDecisionDTO DenyCancellation(Guid requestId, [FromBody] DenyCancellationDTO dto)
        => cancellationService.DenyCancellation(requestId, CurrentUserId(), dto.Reason);

    private string CurrentUserId()
        => HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
}