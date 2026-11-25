namespace Service.DTOs;

// DTOs for user story #28: an employee cancels their own request

// One of the employee's own requests, with what they can do with it
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

// The result of the employee cancelling a request
public record HolidayCancellationDTO(
    Guid Id,
    string Status,          // "cancelled" (pending withdrawn) or "cancellation_requested" (approved holiday)
    bool HoursReturned,     // true when the hours are back in the balance right away
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
    string Status,          // "cancelled" (approved) or "approved" (denied: the holiday stays)
    string DecidedBy,
    DateTime DecidedAt,
    string? Reason,
    decimal HoursReturned);