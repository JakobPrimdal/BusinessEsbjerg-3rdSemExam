namespace Service.DTOs;

public record HolidayBalanceDTO(
    string UserId,
    decimal Weeks1To5Hours,
    decimal Week6Hours,
    List<HolidayAccountDTO> Accounts);

public record HolidayAccountDTO(
    Guid Id,
    string HolidayType,
    DateOnly EarningStart,
    DateOnly EarningEnd,
    DateOnly UsageStart,
    DateOnly UsageEnd,
    DateOnly TransferDeadline,
    string Status,
    decimal BalanceHours,
    List<MonthlyHolidayDTO> Months);

public record MonthlyHolidayDTO(
    DateOnly Month,
    decimal Hours,
    decimal? WeeklyHours);

public record CreateWorkHourDTO(
    string UserId,
    DateOnly ValidFrom,
    decimal MonHours,
    decimal TueHours,
    decimal WedHours,
    decimal ThuHours,
    decimal FriHours,
    decimal SatHours,
    decimal SunHours);