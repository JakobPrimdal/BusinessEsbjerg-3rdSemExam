using Infrastructure.Entities.Enums;

namespace Service.DTOs;

    public record ClosedDayResponseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public ClosedDayType Type { get; set; }
        public DateOnly Date { get; set; }
        public bool IsRecurring { get; set; }
        public DateOnly? RecurringUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public record ClosedDayRequestDTO
    {
        public string Name { get; set; } = "";

        public ClosedDayType Type { get; set; }

        public DateOnly Date { get; set; }

        public bool IsRecurring { get; set; }

        public DateOnly? RecurringUntil { get; set; }
    }