using LinqToDB.Mapping;

namespace Infrastructure.Entities;

[Table("HolidayRuleSet")]
public class HolidayRuleSet
{
    [PrimaryKey]
    public Guid Id { get; set; }
    
    [Column, NotNull]
    public DateOnly ValidFrom { get; set; }

    [Column("CreatedBy")]
    public string? CreatedBy { get; set; }

    [Column, NotNull]
    public DateTime CreatedAt { get; set; }


    
    // Earning rates

    // Holiday earned per month for weeks 1-5.
    // Expressed as holiday days per month.
    [Column, NotNull]
    public decimal Weeks1To5EarningRate { get; set; }

    // Holiday earned per month for week 6.
    // Expressed as hours per month.
    [Column, NotNull]
    public decimal Week6EarningRate { get; set; }

    
    // Earning period - weeks 1-5

    [Column, NotNull]
    public int Weeks1To5EarningStartDay { get; set; }
    
    [Column, NotNull]
    public int Weeks1To5EarningStartMonth { get; set; }

    [Column, NotNull]
    public int Weeks1To5EarningEndDay { get; set; }
    
    [Column, NotNull]
    public int Weeks1To5EarningEndMonth { get; set; }

    [Column, NotNull]
    public int Weeks1To5EarningEndYearOffset { get; set; }

    
    // Earning period - week 6

    [Column, NotNull]
    public int Week6EarningStartDay { get; set; }
    
    [Column, NotNull]
    public int Week6EarningStartMonth { get; set; }

    [Column, NotNull]
    public int Week6EarningEndDay { get; set; }
    
    [Column, NotNull]
    public int Week6EarningEndMonth { get; set; }

    [Column, NotNull]
    public int Week6EarningEndYearOffset { get; set; }


    // Usage period - weeks 1-5

    [Column, NotNull]
    public int Weeks1To5UsageStartDay { get; set; }
    
    [Column, NotNull]
    public int Weeks1To5UsageStartMonth { get; set; }

    [Column, NotNull]
    public int Weeks1To5UsageStartYearOffset { get; set; }

    [Column, NotNull]
    public int Weeks1To5UsageEndDay { get; set; }
    
    [Column, NotNull]
    public int Weeks1To5UsageEndMonth { get; set; }

    [Column, NotNull]
    public int Weeks1To5UsageEndYearOffset { get; set; }


    // Usage period - week 6

    [Column, NotNull]
    public int Week6UsageStartDay { get; set; }

    [Column, NotNull]
    public int Week6UsageStartMonth { get; set; }

    [Column, NotNull]
    public int Week6UsageStartYearOffset { get; set; }

    [Column, NotNull]
    public int Week6UsageEndDay { get; set; }
    
    [Column, NotNull]
    public int Week6UsageEndMonth { get; set; }

    [Column, NotNull]
    public int Week6UsageEndYearOffset { get; set; }


    // Main holiday period

    [Column, NotNull]
    public int MainHolidayStartDay { get; set; }
    
    [Column, NotNull]
    public int MainHolidayStartMonth { get; set; }
    
    [Column, NotNull]
    public int MainHolidayEndDay { get; set; }
    
    [Column, NotNull]
    public int MainHolidayEndMonth { get; set; }


    // Holiday transfer

    [Column, NotNull]
    public bool HolidayTransferEnabled { get; set; }

    [Column, NotNull]
    public int MaximumTransferDays { get; set; }

    [Column, NotNull]
    public int Weeks1To5TransferDeadlineDay { get; set; }
    
    [Column, NotNull]
    public int Weeks1To5TransferDeadlineMonth { get; set; }

    [Column, NotNull]
    public int Weeks1To5TransferDeadlineYearOffset { get; set; }

    [Column, NotNull]
    public int Week6TransferDeadlineDay { get; set; }
    
    [Column, NotNull]
    public int Week6TransferDeadlineMonth { get; set; }

    [Column, NotNull]
    public int Week6TransferDeadlineYearOffset { get; set; }


    // Other rules

    [Column, NotNull]
    public int SickWaitingDays { get; set; }

    [Column, NotNull]
    public int MainHolidayNoticePeriodMonths { get; set; }

    [Column, NotNull]
    public int OtherHolidayNoticePeriodMonths { get; set; }
}
