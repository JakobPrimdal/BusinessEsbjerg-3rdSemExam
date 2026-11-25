namespace Service.DTOs;

public record ActivityLogDTO(
    Guid Id,
    string? UserId,      // null = done by the system
    string Username,
    string Action,       // one of ActivityLogActions
    string Details,
    DateTime CreatedAt); // UTC

public record ActivityLogPageDTO(
    List<ActivityLogDTO> Items,
    int TotalCount,      // matching rows across all pages
    int Page,
    int PageSize);

// For the "filter by user" dropdown
public record ActivityLogUserDTO(
    string UserId,
    string Username);