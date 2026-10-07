using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ReminderSchedulerService : BackgroundService
{
    private const int DefaultIntervalMinutes = 120;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FirebasePushService _firebasePushService;
    private readonly ILogger<ReminderSchedulerService> _logger;

    public ReminderSchedulerService(
        IServiceScopeFactory scopeFactory,
        FirebasePushService firebasePushService,
        ILogger<ReminderSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _firebasePushService = firebasePushService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = DefaultIntervalMinutes;

            try
            {
                intervalMinutes = await ProcessPendingRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reminder scheduler cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task<int> ProcessPendingRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var configuredInterval = await db.SystemConfig
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.DateModified ?? x.DateCreated)
            .Select(x => x.StatisticsDurationMinutes)
            .FirstOrDefaultAsync(cancellationToken);
        var intervalMinutes = configuredInterval > 0 ? configuredInterval : DefaultIntervalMinutes;

        var reminderIds = await db.ReminderLog
            .Where(x => x.IsActive && x.Status == "Pending" &&
                (!x.ScheduledAt.HasValue || x.ScheduledAt <= DateTime.Now))
            .OrderBy(x => x.ScheduledAt)
            .Select(x => x.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var reminderId in reminderIds)
        {
            var claimed = await db.ReminderLog
                .Where(x => x.Id == reminderId && x.Status == "Pending")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Processing")
                    .SetProperty(x => x.DateModified, DateTime.Now)
                    .SetProperty(x => x.ModifiedBy, "reminder-scheduler"), cancellationToken);

            if (claimed == 0) continue;

            var reminder = await db.ReminderLog
                .AsTracking()
                .Include(x => x.ReminderTemplate)
                .Include(x => x.SucklingStatistic).ThenInclude(x => x!.Barn)
                .FirstAsync(x => x.Id == reminderId, cancellationToken);

            if (string.IsNullOrWhiteSpace(reminder.Username))
            {
                reminder.Status = "Failed";
                reminder.ErrorMessage = "Reminder username is required.";
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            var farmId = reminder.SucklingStatistic?.Barn?.FkFarmId;
            if (!farmId.HasValue || !await db.UserFarm.AnyAsync(a => a.IsActive && a.FkFarmId == farmId.Value &&
                a.UserLogin!.IsActive && a.UserLogin.Username == reminder.Username, cancellationToken))
            {
                reminder.Status = "Failed";
                reminder.ErrorMessage = "The recipient is not assigned to the reminder farm.";
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            var tokens = await db.Set<UserDeviceToken>()
                .Where(x => x.IsActive && x.UserLogin != null && x.UserLogin.IsActive &&
                    x.UserLogin.Username == reminder.Username)
                .Select(x => x.DeviceToken)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
            {
                reminder.Status = "Pending";
                reminder.ErrorMessage = $"No active mobile device is registered for {reminder.Username}.";
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var title = reminder.ReminderTemplate?.Name ?? "SmartSheep Reminder";
                var message = reminder.ReminderTemplate?.ReminderText ?? "You have a new SmartSheep reminder.";
                var result = await _firebasePushService.SendAsync(
                    tokens, title, message, "reminder", reminder.Id.ToString(), cancellationToken);

                reminder.Status = result.SuccessCount > 0 ? "Sent" : "Failed";
                reminder.SentAt = result.SuccessCount > 0 ? DateTime.Now : null;
                reminder.ErrorMessage = result.FailureCount > 0
                    ? $"Firebase delivery failed for {result.FailureCount} device(s)."
                    : null;
            }
            catch (Exception ex)
            {
                reminder.Status = "Failed";
                reminder.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                _logger.LogError(ex, "Failed sending reminder {ReminderId}.", reminder.Id);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        await SynchronizeReminderNotificationsAsync(db, cancellationToken);

        return intervalMinutes;
    }

    private static async Task SynchronizeReminderNotificationsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var reminderLogs = await db.ReminderLog
            .Include(x => x.ReminderTemplate)
            .Include(x => x.SucklingStatistic).ThenInclude(x => x!.Barn)
            .Where(x => x.IsActive && (x.Status == "Sent" || x.Status == "Failed"))
            .OrderByDescending(x => x.SentAt ?? x.DateModified)
            .Take(500)
            .ToListAsync(cancellationToken);

        var sourceCodes = reminderLogs.Select(x => $"ReminderLog:{x.Id}").ToList();
        var existing = await db.SystemNotification
            .Where(x => sourceCodes.Contains(x.Code!))
            .Select(x => new { x.Code, x.Username })
            .ToListAsync(cancellationToken);
        var existingKeys = existing
            .Select(x => $"{x.Code}|{x.Username}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notifications = new List<SystemNotification>();

        foreach (var log in reminderLogs.Where(x => !string.IsNullOrWhiteSpace(x.Username)))
        {
            var code = $"ReminderLog:{log.Id}";
            AddNotification(code, log.Username, $"Reminder {log.Status}",
                log.Status == "Sent"
                    ? log.ReminderTemplate?.ReminderText ?? "Reminder was sent successfully."
                    : $"Reminder delivery failed. {log.ErrorMessage}",
                log.Status == "Sent" ? "Success" : "Error",
                $"/ReminderLog/Details/{log.Id}", log.SucklingStatistic?.Barn?.FkFarmId);
        }

        if (notifications.Count == 0) return;

        await db.SystemNotification.AddRangeAsync(notifications, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        void AddNotification(
            string code,
            string username,
            string title,
            string content,
            string type,
            string returnLink, Guid? farmId)
        {
            var key = $"{code}|{username}";
            if (!existingKeys.Add(key)) return;

            notifications.Add(new SystemNotification
            {
                Id = Guid.NewGuid(),
                FkFarmId = farmId,
                Code = code,
                Type = type,
                Username = username,
                Title = title,
                Content = content,
                ReturnLink = returnLink,
                IsRead = false,
                IsActive = true,
                CreatedBy = "reminder-scheduler",
                ModifiedBy = "reminder-scheduler"
            });
        }
    }
}
