using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;

public class HolidayDecisionService(
    HolidayDb db,
    HolidayService holidayService,
    HolidayRequestService requestService,
    TimeProvider clock)
{
    public const string RequestNotFoundError = "Ferieanmodningen blev ikke fundet";
    public const string NotPendingError = "Kun ventende ferieanmodninger kan godkendes eller afvises";
    public const string ReasonRequiredError = "Der skal angives en begrundelse for afvisningen";
    public const string ReasonTooLongError = "Begrundelsen må højst være 500 tegn";

    public const int MaxReasonLength = 500;

    // A denial needs a reason the employee can read. Returns the reason without extra spaces.
    public static string ValidateReason(string? reason)
    {
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length == 0)
            throw new BadRequestException(ReasonRequiredError);
        if (trimmed.Length > MaxReasonLength)
            throw new BadRequestException(ReasonTooLongError);
        return trimmed;
    }

    public List<PendingHolidayRequestDTO> GetPending()
    {
        var pending = (from r in db.HolidayRequests
                       join u in db.Users on r.UserId equals u.UserId
                       where r.Status == HolidayRequestStatus.Pending
                       orderby r.CreatedAt, r.StartDate
                       select new { Request = r, u.Username })
            .ToList();

        return pending
            .Select(p =>
            {
                var r = p.Request;
                var rules = requestService.CheckHolidayRules(
                    r.UserId, r.HolidayType, r.StartDate, r.Hours, requestService.SchedulesFor(r.UserId), r.Id);

                return new PendingHolidayRequestDTO(
                    r.Id, r.UserId, p.Username, r.HolidayType, r.StartDate, r.EndDate, r.Hours, r.CreatedAt,
                    rules.BalanceBefore, rules.BalanceAfter, rules.Error);
            })
            .ToList();
    }
    
    public HolidayDecisionDTO Approve(Guid requestId, string accountantId)
    {
        var request = FindPending(requestId);

        var rules = requestService.CheckHolidayRules(request.UserId, request.HolidayType, request.StartDate,
            request.Hours, requestService.SchedulesFor(request.UserId), request.Id);
        if (rules.Error != null)
            throw new BadRequestException(rules.Error);

        // The status change and the usage lines are saved together, or not at all
        using var transaction = db.BeginTransaction();
        var result = Decide(request, HolidayRequestStatus.Approved, accountantId, reason: null);
        holidayService.RecordUsage(request, accountantId);
        transaction.Commit();

        return result;
    }

    // Denies a pending request with a reason. Nothing is written to the ledger: the hours are no longer
    // planned, so they are back in the balance, and the dates can be requested again.
    public HolidayDecisionDTO Deny(Guid requestId, string accountantId, string? reason)
    {
        var validReason = ValidateReason(reason);
        var request = FindPending(requestId);
        return Decide(request, HolidayRequestStatus.Denied, accountantId, validReason);
    }

    private HolidayRequest FindPending(Guid requestId)
    {
        var request = db.HolidayRequests.FirstOrDefault(r => r.Id == requestId)
                      ?? throw new NotFoundException(RequestNotFoundError);
        if (request.Status != HolidayRequestStatus.Pending)
            throw new BadRequestException(NotPendingError);
        return request;
    }

    // Saves the decision: who decided, when, and why (for a denial)
    private HolidayDecisionDTO Decide(HolidayRequest request, string status, string accountantId, string? reason)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        // Only updates the request if it is still pending, so two accountants clicking at the same time
        // can not both decide it
        var updated = db.HolidayRequests
            .Where(r => r.Id == request.Id && r.Status == HolidayRequestStatus.Pending)
            .Set(r => r.Status, status)
            .Set(r => r.DecidedBy, accountantId)
            .Set(r => r.DecidedAt, now)
            .Set(r => r.DecisionReason, reason)
            .Update();
        if (updated == 0)
            throw new BadRequestException(NotPendingError);

        return new HolidayDecisionDTO(request.Id, status, accountantId, now, reason);
    }
}