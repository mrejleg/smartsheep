using Api.Domain.Entities.Notifications;
using Api.Domain.Entities.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

// Pengiriman ReminderLog Pending lewat Firebase, dipakai bersama oleh:
// - SucklingActivityController.Sync: reminder kandang itu langsung dikirim begitu dibuat dari sync worker;
// - ReminderSchedulerService: setiap SystemConfig.ReminderIntervalMinutes mengirim ulang yang masih Pending.
//
// - Status tetap Pending selama dikirim dan baru diubah setelah ada hasil kirim. Pengiriman diproses satu per satu
//   (lock) dan status dicek ulang tepat sebelum dikirim, sehingga reminder tidak terkirim dua kali dari dua jalur.
// - Gangguan sementara (Firebase gagal/error, belum ada perangkat terdaftar) tetap Pending dan dicoba lagi;
//   Failed hanya bila tidak mungkin terkirim (username kosong, penerima tidak ditugaskan di farm kandang).
// - Reminder berlaku sampai periode berikutnya kandang itu selesai (Barn.StatisticReminderMinutes, dipotong 24.00);
//   lewat dari itu menjadi Expired agar reminder usang tidak terkirim terlambat.
public sealed class ReminderDeliveryService
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    private const int DefaultStatisticReminderMinutes = 120;
    private const string Actor = "reminder-delivery";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FirebasePushService _firebasePushService;
    private readonly ILogger<ReminderDeliveryService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ReminderDeliveryService(
        IServiceScopeFactory scopeFactory,
        FirebasePushService firebasePushService,
        ILogger<ReminderDeliveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _firebasePushService = firebasePushService;
        _logger = logger;
    }

    // Kirim reminder Pending. Sync: sourceVideoCode = kandang SourceVideo itu dan scheduledSince = awal sync, sehingga
    // hanya reminder yang baru dibuat sync itu yang dikirim (pengulangan tugas scheduler). Scheduler: keduanya null.
    // Dibaca tanpa filter farm: dipanggil worker/background (bukan user farm), sama seperti sync.
    public async Task SendPendingAsync(string? sourceVideoCode, DateTime? scheduledSince, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Guid? fkBarnId = null;
            if (sourceVideoCode != null)
            {
                fkBarnId = await db.SourceVideo.IgnoreQueryFilters()
                    .Where(x => x.IsActive && x.Code == sourceVideoCode)
                    .Select(x => x.FkBarnId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!fkBarnId.HasValue) return;
            }

            var reminderIds = await db.ReminderLog.IgnoreQueryFilters()
                .Where(x => x.IsActive && x.Status == Pending &&
                    (!x.ScheduledAt.HasValue || x.ScheduledAt <= DateTime.Now) &&
                    (!scheduledSince.HasValue || x.ScheduledAt >= scheduledSince.Value) &&
                    (!fkBarnId.HasValue || x.SucklingStatistic!.FkBarnId == fkBarnId.Value))
                .OrderBy(x => x.ScheduledAt)
                .Select(x => x.Id)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var reminderId in reminderIds)
            {
                var reminder = await db.ReminderLog.IgnoreQueryFilters()
                    .AsTracking()
                    .Include(x => x.ReminderTemplate)
                    .Include(x => x.SucklingStatistic).ThenInclude(x => x!.Barn)
                    .FirstOrDefaultAsync(x => x.Id == reminderId && x.Status == Pending, cancellationToken);
                if (reminder == null) continue;

                await SendAsync(db, reminder, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            await SynchronizeReminderNotificationsAsync(db, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    // Batas berlaku: akhir periode berikutnya kandang itu, dengan aturan periode yang sama seperti statistik
    public static DateTime? GetExpiresAt(SucklingStatistic? statistic)
    {
        if (statistic?.ActivityTo == null) return null;

        var minutes = statistic.Barn?.StatisticReminderMinutes > 0 ? statistic.Barn.StatisticReminderMinutes : DefaultStatisticReminderMinutes;
        var end = statistic.ActivityTo.Value;
        var nextEnd = end.AddMinutes(minutes);
        var midnight = end.Date.AddDays(1);
        return nextEnd < midnight ? nextEnd : midnight;
    }

    private async Task SendAsync(ApplicationDbContext db, ReminderLog reminder, CancellationToken cancellationToken)
    {
        reminder.ModifiedBy = Actor;
        reminder.DateModified = DateTime.Now;

        if (string.IsNullOrWhiteSpace(reminder.Username))
        {
            reminder.Status = Failed;
            reminder.ErrorMessage = "Reminder username is required.";
            return;
        }

        var expiresAt = GetExpiresAt(reminder.SucklingStatistic);
        if (expiresAt.HasValue && DateTime.Now >= expiresAt.Value)
        {
            reminder.Status = Expired;
            reminder.ErrorMessage = Truncate($"Reminder expired at {expiresAt.Value:yyyy-MM-dd HH:mm} before it could be delivered. " +
                reminder.ErrorMessage);
            return;
        }

        var farmId = reminder.SucklingStatistic?.Barn?.FkFarmId;
        if (!farmId.HasValue || !await db.UserFarm.IgnoreQueryFilters().AnyAsync(a => a.IsActive && a.FkFarmId == farmId.Value &&
            a.UserLogin!.IsActive && a.UserLogin.Username == reminder.Username, cancellationToken))
        {
            reminder.Status = Failed;
            reminder.ErrorMessage = "The recipient is not assigned to the reminder farm.";
            return;
        }

        var tokens = await db.Set<UserDeviceToken>().IgnoreQueryFilters()
            .Where(x => x.IsActive && x.UserLogin != null && x.UserLogin.IsActive &&
                x.UserLogin.Username == reminder.Username)
            .Select(x => x.DeviceToken)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            reminder.ErrorMessage = $"No active mobile device is registered for {reminder.Username}.";
            return;
        }

        try
        {
            var title = reminder.ReminderTemplate?.Name ?? "SmartSheep Reminder";
            var message = reminder.ReminderTemplate?.ReminderText ?? "You have a new SmartSheep reminder.";
            var result = await _firebasePushService.SendAsync(
                tokens, title, message, "reminder", reminder.Id.ToString(), cancellationToken);

            if (result.SuccessCount > 0)
            {
                reminder.Status = Sent;
                reminder.SentAt = DateTime.Now;
            }

            reminder.ErrorMessage = result.FailureCount > 0
                ? $"Firebase delivery failed for {result.FailureCount} device(s)."
                : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            reminder.ErrorMessage = Truncate(ex.Message);
            _logger.LogError(ex, "Failed sending reminder {ReminderId}.", reminder.Id);
        }
    }

    private static string Truncate(string value)
    {
        return value.Length <= 1000 ? value : value[..1000];
    }

    private static async Task SynchronizeReminderNotificationsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var reminderLogs = await db.ReminderLog.IgnoreQueryFilters()
            .Include(x => x.ReminderTemplate)
            .Include(x => x.SucklingStatistic).ThenInclude(x => x!.Barn)
            .Where(x => x.IsActive && (x.Status == Sent || x.Status == Failed))
            .OrderByDescending(x => x.SentAt ?? x.DateModified)
            .Take(500)
            .ToListAsync(cancellationToken);

        var sourceCodes = reminderLogs.Select(x => $"ReminderLog:{x.Id}").ToList();
        var existing = await db.SystemNotification.IgnoreQueryFilters()
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
                log.Status == Sent
                    ? log.ReminderTemplate?.ReminderText ?? "Reminder was sent successfully."
                    : $"Reminder delivery failed. {log.ErrorMessage}",
                log.Status == Sent ? "Success" : "Error",
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
