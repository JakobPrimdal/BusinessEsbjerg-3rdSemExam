namespace Service.DTOs;

public record CreateHolidayRequestDTO(
    DateOnly StartDate,
    DateOnly EndDate,
    string HolidayType);

public record HolidayRequestPreviewDTO(
    int WorkingDays,
    decimal Hours,          // what the request costs
    decimal BalanceBefore,  // projected balance on the start date before this request
    decimal BalanceAfter,   // after it (negative means holiday debt)
    decimal MaxDebt,       
    bool CanRequest,
    string? Error);

public record HolidayRequestDTO(
    Guid Id,
    string HolidayType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string Status,
    DateTime CreatedAt);