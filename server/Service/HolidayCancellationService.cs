using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;

namespace Service;
public class HolidayCancellationService(HolidayDb db, HolidayService holidayService, TimeProvider clock)
{
    public const string RequestNotFoundError = "Ferieanmodningen blev ikke fundet";
    public const string AlreadyStartedError = "Ferien er allerede startet og kan ikke annulleres";
    public const string CannotCancelError = "Kun ventende eller godkendte ferieanmodninger kan annulleres";
    public const string NotCancellationRequestedError = "Der er ingen ventende annullering for denne ferie";
    public const string StatusChangedError = "Ferieanmodningen er blevet ændret. Prøv igen";

    // Employee 

    public List<MyHolidayRequestDTO> GetMyRequests(string userId)
    {
        var today = Today();
        return db.HolidayRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToList()
            .Select(r => new MyHolidayRequestDTO(
                r.Id, r.HolidayType, r.StartDate, r.EndDate, r.Hours, r.Status, r.CreatedAt,
                r.DecisionReason, r.CancellationDecisionReason,
                CanCancel: CanCancel(r, today),
                CancelNeedsApproval: r.Status == HolidayRequestStatus.Approved))
            .ToList();
    }

    public static bool CanCancel(HolidayRequest request, DateOnly today)
        => request.Status == HolidayRequestStatus.Pending
           || (request.Status == HolidayRequestStatus.Approved && request.StartDate > today);

    public HolidayCancellationDTO Cancel(string userId, Guid requestId)
    {
        // Another employee's request is "not found", so ids of other people's requests are not revealed
        var request = db.HolidayRequests.FirstOrDefault(r => r.Id == requestId && r.UserId == userId)
                      ?? throw new NotFoundException(RequestNotFoundError);

        if (request.Status == HolidayRequestStatus.Pending)
            return WithdrawPending(request);

        if (request.Status == HolidayRequestStatus.Approved)
        {
            if (request.StartDate <= Today())
                throw new BadRequestException(AlreadyStartedError);
            return RequestCancellation(request);
        }

        throw new BadRequestException(CannotCancelError);
    }

    private HolidayCancellationDTO WithdrawPending(HolidayRequest request)
    {
        // Only if it is still pending, so it can not be withdrawn while the accountant approves it
        var updated = db.HolidayRequests
            .Where(r => r.Id == request.Id && r.Status == HolidayRequestStatus.Pending)
            .Set(r => r.Status, HolidayRequestStatus.Cancelled)
            .Update();
        if (updated == 0)
            throw new BadRequestException(StatusChangedError);

        return new HolidayCancellationDTO(request.Id, HolidayRequestStatus.Cancelled, HoursReturned: true,
            request.Hours);
    }

    private HolidayCancellationDTO RequestCancellation(HolidayRequest request)
    {
        var updated = db.HolidayRequests
            .Where(r => r.Id == request.Id && r.Status == HolidayRequestStatus.Approved)
            .Set(r => r.Status, HolidayRequestStatus.CancellationRequested)
            .Set(r => r.CancellationRequestedAt, clock.GetUtcNow().UtcDateTime)
            // a new request clears an earlier denied one
            .Set(r => r.CancellationDecidedBy, (string?)null)
            .Set(r => r.CancellationDecidedAt, (DateTime?)null)
            .Set(r => r.CancellationDecisionReason, (string?)null)
            .Update();
        if (updated == 0)
            throw new BadRequestException(StatusChangedError);

        return new HolidayCancellationDTO(request.Id, HolidayRequestStatus.CancellationRequested,
            HoursReturned: false, request.Hours);
    }

    // Accountant 

    public List<CancellationRequestDTO> GetCancellationRequests()
        => (from r in db.HolidayRequests
            join u in db.Users on r.UserId equals u.UserId
            where r.Status == HolidayRequestStatus.CancellationRequested
            orderby r.CancellationRequestedAt, r.StartDate
            select new { Request = r, u.Username })
            .ToList()
            .Select(p => new CancellationRequestDTO(
                p.Request.Id, p.Request.UserId, p.Username, p.Request.HolidayType,
                p.Request.StartDate, p.Request.EndDate, p.Request.Hours,
                p.Request.CancellationRequestedAt ?? p.Request.CreatedAt))
            .ToList();

    public CancellationDecisionDTO ApproveCancellation(Guid requestId, string accountantId)
    {
        var request = FindCancellationRequested(requestId);
        // Holiday that has started is being taken, so its hours can not be given back
        if (request.StartDate <= Today())
            throw new BadRequestException(AlreadyStartedError);

        var now = clock.GetUtcNow().UtcDateTime;

        // The status change and the reversal lines are saved together, or not at all
        using var transaction = db.BeginTransaction();
        var updated = db.HolidayRequests
            .Where(r => r.Id == request.Id && r.Status == HolidayRequestStatus.CancellationRequested)
            .Set(r => r.Status, HolidayRequestStatus.Cancelled)
            .Set(r => r.CancellationDecidedBy, accountantId)
            .Set(r => r.CancellationDecidedAt, now)
            .Set(r => r.CancellationDecisionReason, (string?)null)
            .Update();
        if (updated == 0)
            throw new BadRequestException(NotCancellationRequestedError);

        var returned = holidayService.RecordReversal(request, accountantId);
        transaction.Commit();

        return new CancellationDecisionDTO(request.Id, HolidayRequestStatus.Cancelled, accountantId, now,
            Reason: null, returned);
    }

    // The holiday stays approved, and the employee can read why
    public CancellationDecisionDTO DenyCancellation(Guid requestId, string accountantId, string? reason)
    {
        var validReason = HolidayDecisionService.ValidateReason(reason);
        var request = FindCancellationRequested(requestId);
        var now = clock.GetUtcNow().UtcDateTime;

        var updated = db.HolidayRequests
            .Where(r => r.Id == request.Id && r.Status == HolidayRequestStatus.CancellationRequested)
            .Set(r => r.Status, HolidayRequestStatus.Approved)
            .Set(r => r.CancellationDecidedBy, accountantId)
            .Set(r => r.CancellationDecidedAt, now)
            .Set(r => r.CancellationDecisionReason, validReason)
            .Update();
        if (updated == 0)
            throw new BadRequestException(NotCancellationRequestedError);

        return new CancellationDecisionDTO(request.Id, HolidayRequestStatus.Approved, accountantId, now,
            validReason, HoursReturned: 0);
    }

    private HolidayRequest FindCancellationRequested(Guid requestId)
    {
        var request = db.HolidayRequests.FirstOrDefault(r => r.Id == requestId)
                      ?? throw new NotFoundException(RequestNotFoundError);
        if (request.Status != HolidayRequestStatus.CancellationRequested)
            throw new BadRequestException(NotCancellationRequestedError);
        return request;
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}