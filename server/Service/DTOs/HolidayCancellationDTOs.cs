namespace Service.DTOs;

public record MyHolidayRequestDTO(
    Guid Id,
    string HolidayType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Status,
    DateTime CreatedAt,
    string? DecisionReason,              // why it was denied
    string? CancellationDecisionReason,  // why a cancellation was denied
    bool CanCancel,                      // pending, or approved and not started
    bool CancelNeedsApproval);           // true for approved holiday: the accountant must agree


public record HolidayCancellationDTO(
    Guid Id,
    string Status,          // "cancelled" (pending withdrawn) or "cancellation_requested" (approved holiday)
    bool HoursReturned,     
    decimal Hours);

// An approved holiday the employee wants to cancel, waiting for the accountant
public record CancellationRequestDTO(
    Guid Id,
    string UserId,
    string Username,
    string HolidayType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    DateTime CancellationRequestedAt);

public record DenyCancellationDTO(string? Reason);

public record CancellationDecisionDTO(
    Guid Id,
    string Status,          
    string DecidedBy,
    DateTime DecidedAt,
    string? Reason,
    decimal HoursReturned);