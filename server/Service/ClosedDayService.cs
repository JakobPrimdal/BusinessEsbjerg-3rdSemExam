using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Service.DTOs;
using Service.Exceptions;
using Service.Interfaces;

namespace Service;

public class ClosedDayService(HolidayDb db) : IClosedDayService
{
    public List<ClosedDayResponseDTO> GetAllClosedDays()
    {
        var closedDays = db.ClosedDays.OrderBy(cd => cd.Date).ToList();
        
        return ToResponses(closedDays);
    }

    public ClosedDayResponseDTO CreateClosedDay(ClosedDayRequestDTO dto)
    {
        ValidateRequest(dto);

        var closedDay = new ClosedDay
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Type = dto.Type,
            Date = dto.Date,
            IsRecurring = dto.IsRecurring,
            RecurringUntil = dto.IsRecurring ? dto.RecurringUntil : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false
        };

        db.Insert(closedDay);

        return ToResponse(closedDay);
    }

    public ClosedDayResponseDTO UpdateClosedDay(Guid id, ClosedDayRequestDTO dto)
    {
        ValidateRequest(dto, id);

        var closedDay = db.ClosedDays.FirstOrDefault(cd => cd.Id == id && !cd.IsDeleted)
                        ?? throw new NotFoundException("Closed day with id " + id + " not found");

        closedDay.Name = dto.Name.Trim();
        closedDay.Type = dto.Type;
        closedDay.Date = dto.Date;
        closedDay.IsRecurring = dto.IsRecurring;
        closedDay.RecurringUntil = dto.IsRecurring ? dto.RecurringUntil : null;
        closedDay.UpdatedAt = DateTime.UtcNow;

        db.Update(closedDay);

        return ToResponse(closedDay);
    }

    public void ToggleIsDeleted(Guid id)
    {
        var closedDay = db.ClosedDays.FirstOrDefault(cd => cd.Id == id)
            ?? throw new NotFoundException("Closed day with id " + id + " was not found");

        closedDay.IsDeleted = !closedDay.IsDeleted;
        closedDay.UpdatedAt = DateTime.UtcNow;

        db.Update(closedDay);

    }
    
    private ClosedDayResponseDTO ToResponse(ClosedDay entity)
    {
        return new ClosedDayResponseDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Date = entity.Date,
            IsRecurring = entity.IsRecurring,
            RecurringUntil = entity.RecurringUntil,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private List<ClosedDayResponseDTO> ToResponses(List<ClosedDay> entities)
    {
        return entities.Select(ToResponse).ToList();
    }
    
    private void ValidateRequest(ClosedDayRequestDTO dto, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Name is required.");

        if (dto.Name.Trim().Length > 200)
            throw new BadRequestException("Name cannot be longer than 200 characters.");

        if (!Enum.IsDefined(dto.Type))
            throw new BadRequestException("Type is not a valid closed day type.");

        if (!dto.IsRecurring && dto.RecurringUntil is not null)
            throw new BadRequestException("RecurringUntil can only be set when IsRecurring is true.");

        if (dto.RecurringUntil is not null && dto.RecurringUntil < dto.Date)
            throw new BadRequestException("RecurringUntil must be on or after Date.");

        var duplicateExists = db.ClosedDays.Any(x =>
            !x.IsDeleted &&
            x.Id != excludeId &&
            x.Date == dto.Date &&
            x.Type == dto.Type);

        if (duplicateExists)
            throw new BadRequestException("A closed day of this type already exists on this date.");
    }
}