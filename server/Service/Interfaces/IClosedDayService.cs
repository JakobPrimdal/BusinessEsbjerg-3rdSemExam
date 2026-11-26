using Service.DTOs;

namespace Service.Interfaces;

public interface IClosedDayService
{
    public List<ClosedDayResponseDTO> GetAllClosedDays();

    public ClosedDayResponseDTO CreateClosedDay(ClosedDayRequestDTO dto);

    public ClosedDayResponseDTO UpdateClosedDay(Guid id, ClosedDayRequestDTO dto);

    public void ToggleIsDeleted(Guid id);
}