using Service.DTOs;

namespace Service.Interfaces;

public interface IActivityLogService
{
    // Records that userId (null = the system) did an action. Call it from a service
    // right after the change succeeded; inside the same transaction if there is one.
    void Log(string? userId, string action, string details);

    // Newest first. from is inclusive, to is exclusive. Both are optional.
    ActivityLogPageDTO GetLogs(string? userId, DateTimeOffset? from, DateTimeOffset? to, int page);

    List<ActivityLogUserDTO> GetUsers();
}