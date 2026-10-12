using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

// Jembatan reminder yang gagal: setiap SystemConfig.ReminderIntervalMinutes (dibaca ulang tiap putaran) mengirim ulang
// ReminderLog yang masih Pending lewat ReminderDeliveryService. Reminder baru sudah langsung dikirim saat sync.
public sealed class ReminderSchedulerService : BackgroundService
{
    private const int DefaultIntervalMinutes = 10;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReminderDeliveryService _reminderDeliveryService;
    private readonly ILogger<ReminderSchedulerService> _logger;

    public ReminderSchedulerService(
        IServiceScopeFactory scopeFactory,
        ReminderDeliveryService reminderDeliveryService,
        ILogger<ReminderSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _reminderDeliveryService = reminderDeliveryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = DefaultIntervalMinutes;

            try
            {
                intervalMinutes = await GetIntervalMinutesAsync(stoppingToken);
                await _reminderDeliveryService.SendPendingAsync(null, null, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reminder scheduler cycle failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> GetIntervalMinutesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var interval = await db.SystemConfig
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.DateModified ?? x.DateCreated)
            .Select(x => x.ReminderIntervalMinutes)
            .FirstOrDefaultAsync(cancellationToken);
        return interval > 0 ? interval : DefaultIntervalMinutes;
    }
}
