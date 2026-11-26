namespace Service.DTOs;

public record CreateHolidayRequestDTO(
    DateOnly StartDate,
    DateOnly EndDate,
    string HolidayType);

// A broken holiday rule that does not block the request
public record HolidayRuleWarningDTO(
    string Code,            // HolidayRequestService.ShortNoticeWarning or MainHolidayWarning
    string Message);

public record HolidayRequestPreviewDTO(
    int WorkingDays,
    decimal Hours,          // what the request costs
    decimal BalanceBefore,  // projected balance on the start date before this request
    decimal BalanceAfter,   // after it (negative means holiday debt)
    decimal MaxDebt,        // 0 for week 6: it can not be taken in advance
    bool CanRequest,        // false when Error is set
    string? Error,          // a broken rule that blocks the request
    List<HolidayRuleWarningDTO> Warnings);

public record HolidayRequestDTO(
    Guid Id,
    string HolidayType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Status,
    DateTime CreatedAt,
    List<HolidayRuleWarningDTO> Warnings);

public record PendingHolidayRequestDTO(
    Guid Id,
    string UserId,
    string Username,
    string HolidayType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    DateTime CreatedAt,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Problem,        // blocks approval
    List<HolidayRuleWarningDTO> Warnings); // shown to the accountant, approval is still possible

public record DenyHolidayRequestDTO(string? Reason);

public record HolidayDecisionDTO(
    Guid Id,
    string Status,
    string DecidedBy,
    DateTime DecidedAt,
    string? DecisionReason);