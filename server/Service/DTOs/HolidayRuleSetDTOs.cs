using Infrastructure.Entities;

namespace Service.DTOs;

public class HolidayRuleSetDTOs
{
    
    public class HolidayRuleSetRequestDTO
    {
        public DateOnly ValidFrom { get; set; }

        // Earning rates
        public decimal Weeks1To5EarningRate { get; set; }
        public decimal Week6EarningRate { get; set; }

        // Earning period - weeks 1-5
        public int Weeks1To5EarningStartDay { get; set; }
        public int Weeks1To5EarningStartMonth { get; set; }
        public int Weeks1To5EarningEndDay { get; set; }
        public int Weeks1To5EarningEndMonth { get; set; }
        public int Weeks1To5EarningEndYearOffset { get; set; }

        // Earning period - week 6
        public int Week6EarningStartDay { get; set; }
        public int Week6EarningStartMonth { get; set; }
        public int Week6EarningEndDay { get; set; }
        public int Week6EarningEndMonth { get; set; }
        public int Week6EarningEndYearOffset { get; set; }

        // Usage period - weeks 1-5
        public int Weeks1To5UsageStartDay { get; set; }
        public int Weeks1To5UsageStartMonth { get; set; }
        public int Weeks1To5UsageStartYearOffset { get; set; }
        public int Weeks1To5UsageEndDay { get; set; }
        public int Weeks1To5UsageEndMonth { get; set; }
        public int Weeks1To5UsageEndYearOffset { get; set; }

        // Usage period - week 6
        public int Week6UsageStartDay { get; set; }
        public int Week6UsageStartMonth { get; set; }
        public int Week6UsageStartYearOffset { get; set; }
        public int Week6UsageEndDay { get; set; }
        public int Week6UsageEndMonth { get; set; }
        public int Week6UsageEndYearOffset { get; set; }

        // Main holiday period
        public int MainHolidayStartDay { get; set; }
        public int MainHolidayStartMonth { get; set; }
        public int MainHolidayEndDay { get; set; }
        public int MainHolidayEndMonth { get; set; }

        // Holiday transfer
        public bool HolidayTransferEnabled { get; set; }
        public int MaximumTransferDays { get; set; }
        public int Weeks1To5TransferDeadlineDay { get; set; }
        public int Weeks1To5TransferDeadlineMonth { get; set; }
        public int Weeks1To5TransferDeadlineYearOffset { get; set; }
        public int Week6TransferDeadlineDay { get; set; }
        public int Week6TransferDeadlineMonth { get; set; }
        public int Week6TransferDeadlineYearOffset { get; set; }

        // Other rules
        public int SickWaitingDays { get; set; }
        public int MainHolidayNoticePeriodMonths { get; set; }
        public int OtherHolidayNoticePeriodMonths { get; set; }
    }
    

    public class HolidayRuleSetResponseDTO
    {
        public Guid Id { get; set; }
        public DateOnly ValidFrom { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Earning rates
        public decimal Weeks1To5EarningRate { get; set; }
        public decimal Week6EarningRate { get; set; }

        // Earning period - weeks 1-5
        public int Weeks1To5EarningStartDay { get; set; }
        public int Weeks1To5EarningStartMonth { get; set; }
        public int Weeks1To5EarningEndDay { get; set; }
        public int Weeks1To5EarningEndMonth { get; set; }
        public int Weeks1To5EarningEndYearOffset { get; set; }

        // Earning period - week 6
        public int Week6EarningStartDay { get; set; }
        public int Week6EarningStartMonth { get; set; }
        public int Week6EarningEndDay { get; set; }
        public int Week6EarningEndMonth { get; set; }
        public int Week6EarningEndYearOffset { get; set; }

        // Usage period - weeks 1-5
        public int Weeks1To5UsageStartDay { get; set; }
        public int Weeks1To5UsageStartMonth { get; set; }
        public int Weeks1To5UsageStartYearOffset { get; set; }
        public int Weeks1To5UsageEndDay { get; set; }
        public int Weeks1To5UsageEndMonth { get; set; }
        public int Weeks1To5UsageEndYearOffset { get; set; }

        // Usage period - week 6
        public int Week6UsageStartDay { get; set; }
        public int Week6UsageStartMonth { get; set; }
        public int Week6UsageStartYearOffset { get; set; }
        public int Week6UsageEndDay { get; set; }
        public int Week6UsageEndMonth { get; set; }
        public int Week6UsageEndYearOffset { get; set; }

        // Main holiday period
        public int MainHolidayStartDay { get; set; }
        public int MainHolidayStartMonth { get; set; }
        public int MainHolidayEndDay { get; set; }
        public int MainHolidayEndMonth { get; set; }

        // Holiday transfer
        public bool HolidayTransferEnabled { get; set; }
        public int MaximumTransferDays { get; set; }
        public int Weeks1To5TransferDeadlineDay { get; set; }
        public int Weeks1To5TransferDeadlineMonth { get; set; }
        public int Weeks1To5TransferDeadlineYearOffset { get; set; }
        public int Week6TransferDeadlineDay { get; set; }
        public int Week6TransferDeadlineMonth { get; set; }
        public int Week6TransferDeadlineYearOffset { get; set; }

        // Other rules
        public int SickWaitingDays { get; set; }
        public int MainHolidayNoticePeriodMonths { get; set; }
        public int OtherHolidayNoticePeriodMonths { get; set; }
    }

    public static HolidayRuleSet MapToEntity(HolidayRuleSetDTOs.HolidayRuleSetRequestDTO dto)
    {
        return new HolidayRuleSet
        {
            ValidFrom = dto.ValidFrom,

            // Earning rates
            Weeks1To5EarningRate = dto.Weeks1To5EarningRate,
            Week6EarningRate = dto.Week6EarningRate,

            // Earning period - weeks 1-5
            Weeks1To5EarningStartDay = dto.Weeks1To5EarningStartDay,
            Weeks1To5EarningStartMonth = dto.Weeks1To5EarningStartMonth,
            Weeks1To5EarningEndDay = dto.Weeks1To5EarningEndDay,
            Weeks1To5EarningEndMonth = dto.Weeks1To5EarningEndMonth,
            Weeks1To5EarningEndYearOffset = dto.Weeks1To5EarningEndYearOffset,

            // Earning period - week 6
            Week6EarningStartDay = dto.Week6EarningStartDay,
            Week6EarningStartMonth = dto.Week6EarningStartMonth,
            Week6EarningEndDay = dto.Week6EarningEndDay,
            Week6EarningEndMonth = dto.Week6EarningEndMonth,
            Week6EarningEndYearOffset = dto.Week6EarningEndYearOffset,

            // Usage period - weeks 1-5
            Weeks1To5UsageStartDay = dto.Weeks1To5UsageStartDay,
            Weeks1To5UsageStartMonth = dto.Weeks1To5UsageStartMonth,
            Weeks1To5UsageStartYearOffset = dto.Weeks1To5UsageStartYearOffset,
            Weeks1To5UsageEndDay = dto.Weeks1To5UsageEndDay,
            Weeks1To5UsageEndMonth = dto.Weeks1To5UsageEndMonth,
            Weeks1To5UsageEndYearOffset = dto.Weeks1To5UsageEndYearOffset,

            // Usage period - week 6
            Week6UsageStartDay = dto.Week6UsageStartDay,
            Week6UsageStartMonth = dto.Week6UsageStartMonth,
            Week6UsageStartYearOffset = dto.Week6UsageStartYearOffset,
            Week6UsageEndDay = dto.Week6UsageEndDay,
            Week6UsageEndMonth = dto.Week6UsageEndMonth,
            Week6UsageEndYearOffset = dto.Week6UsageEndYearOffset,

            // Main holiday period
            MainHolidayStartDay = dto.MainHolidayStartDay,
            MainHolidayStartMonth = dto.MainHolidayStartMonth,
            MainHolidayEndDay = dto.MainHolidayEndDay,
            MainHolidayEndMonth = dto.MainHolidayEndMonth,

            // Holiday transfer
            HolidayTransferEnabled = dto.HolidayTransferEnabled,
            MaximumTransferDays = dto.MaximumTransferDays,
            Weeks1To5TransferDeadlineDay = dto.Weeks1To5TransferDeadlineDay,
            Weeks1To5TransferDeadlineMonth = dto.Weeks1To5TransferDeadlineMonth,
            Weeks1To5TransferDeadlineYearOffset = dto.Weeks1To5TransferDeadlineYearOffset,
            Week6TransferDeadlineDay = dto.Week6TransferDeadlineDay,
            Week6TransferDeadlineMonth = dto.Week6TransferDeadlineMonth,
            Week6TransferDeadlineYearOffset = dto.Week6TransferDeadlineYearOffset,

            // Other rules
            SickWaitingDays = dto.SickWaitingDays,
            MainHolidayNoticePeriodMonths = dto.MainHolidayNoticePeriodMonths,
            OtherHolidayNoticePeriodMonths = dto.OtherHolidayNoticePeriodMonths
        };
    }

    public static HolidayRuleSetResponseDTO MapToResponseDTO(HolidayRuleSet ruleSet)
    {
        return new HolidayRuleSetResponseDTO
        {
            Id = ruleSet.Id,
            ValidFrom = ruleSet.ValidFrom,
            CreatedBy = ruleSet.CreatedBy,
            CreatedAt = ruleSet.CreatedAt,

            // Earning rates
            Weeks1To5EarningRate = ruleSet.Weeks1To5EarningRate,
            Week6EarningRate = ruleSet.Week6EarningRate,

            // Earning period - weeks 1-5
            Weeks1To5EarningStartDay = ruleSet.Weeks1To5EarningStartDay,
            Weeks1To5EarningStartMonth = ruleSet.Weeks1To5EarningStartMonth,
            Weeks1To5EarningEndDay = ruleSet.Weeks1To5EarningEndDay,
            Weeks1To5EarningEndMonth = ruleSet.Weeks1To5EarningEndMonth,
            Weeks1To5EarningEndYearOffset = ruleSet.Weeks1To5EarningEndYearOffset,

            // Earning period - week 6
            Week6EarningStartDay = ruleSet.Week6EarningStartDay,
            Week6EarningStartMonth = ruleSet.Week6EarningStartMonth,
            Week6EarningEndDay = ruleSet.Week6EarningEndDay,
            Week6EarningEndMonth = ruleSet.Week6EarningEndMonth,
            Week6EarningEndYearOffset = ruleSet.Week6EarningEndYearOffset,

            // Usage period - weeks 1-5
            Weeks1To5UsageStartDay = ruleSet.Weeks1To5UsageStartDay,
            Weeks1To5UsageStartMonth = ruleSet.Weeks1To5UsageStartMonth,
            Weeks1To5UsageStartYearOffset = ruleSet.Weeks1To5UsageStartYearOffset,
            Weeks1To5UsageEndDay = ruleSet.Weeks1To5UsageEndDay,
            Weeks1To5UsageEndMonth = ruleSet.Weeks1To5UsageEndMonth,
            Weeks1To5UsageEndYearOffset = ruleSet.Weeks1To5UsageEndYearOffset,

            // Usage period - week 6
            Week6UsageStartDay = ruleSet.Week6UsageStartDay,
            Week6UsageStartMonth = ruleSet.Week6UsageStartMonth,
            Week6UsageStartYearOffset = ruleSet.Week6UsageStartYearOffset,
            Week6UsageEndDay = ruleSet.Week6UsageEndDay,
            Week6UsageEndMonth = ruleSet.Week6UsageEndMonth,
            Week6UsageEndYearOffset = ruleSet.Week6UsageEndYearOffset,

            // Main holiday period
            MainHolidayStartDay = ruleSet.MainHolidayStartDay,
            MainHolidayStartMonth = ruleSet.MainHolidayStartMonth,
            MainHolidayEndDay = ruleSet.MainHolidayEndDay,
            MainHolidayEndMonth = ruleSet.MainHolidayEndMonth,

            // Holiday transfer
            HolidayTransferEnabled = ruleSet.HolidayTransferEnabled,
            MaximumTransferDays = ruleSet.MaximumTransferDays,
            Weeks1To5TransferDeadlineDay = ruleSet.Weeks1To5TransferDeadlineDay,
            Weeks1To5TransferDeadlineMonth = ruleSet.Weeks1To5TransferDeadlineMonth,
            Weeks1To5TransferDeadlineYearOffset = ruleSet.Weeks1To5TransferDeadlineYearOffset,
            Week6TransferDeadlineDay = ruleSet.Week6TransferDeadlineDay,
            Week6TransferDeadlineMonth = ruleSet.Week6TransferDeadlineMonth,
            Week6TransferDeadlineYearOffset = ruleSet.Week6TransferDeadlineYearOffset,

            // Other rules
            SickWaitingDays = ruleSet.SickWaitingDays,
            MainHolidayNoticePeriodMonths = ruleSet.MainHolidayNoticePeriodMonths,
            OtherHolidayNoticePeriodMonths = ruleSet.OtherHolidayNoticePeriodMonths
        };
    }
}