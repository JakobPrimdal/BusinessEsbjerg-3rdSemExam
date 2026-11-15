using Service;

namespace Api.Jobs;

public class MonthlyHolidayScheduler(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<MonthlyHolidayScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var holidayService = scope.ServiceProvider.GetRequiredService<HolidayService>();

                var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                var lastMonth = HolidayService.FirstDayOfMonth(today).AddMonths(-1);

                var count = holidayService.AddMonthlyHolidayForAll(lastMonth);
                logger.LogInformation("Monthly holiday for {Month}: added for {Count} user(s)", lastMonth, count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monthly holiday job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}