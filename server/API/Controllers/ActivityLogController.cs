using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.DTOs;
using Service.Interfaces;

namespace Api.Controllers;

// Read-only on purpose: the activity log is never edited or deleted through the API.
// New rows are written by the services (IActivityLogService.Log), not by the frontend.
[Authorize(Roles = "Admin")]
public class ActivityLogController(IActivityLogService activityLogService) : ControllerBase
{
    // from/to are points in time (the frontend sends local midnight as ISO), to is exclusive
    [HttpGet("activitylog/" + nameof(GetActivityLog))]
    public ActivityLogPageDTO GetActivityLog(
        [FromQuery] string? userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1)
        => activityLogService.GetLogs(userId, from, to, page);

    [HttpGet("activitylog/" + nameof(GetUsers))]
    public List<ActivityLogUserDTO> GetUsers()
        => activityLogService.GetUsers();
}