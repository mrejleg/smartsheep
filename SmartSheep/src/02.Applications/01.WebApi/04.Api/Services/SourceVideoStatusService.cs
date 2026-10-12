using Api.Domain.Entities.MasterDatas;
using Api.Domain.Entities.Notifications;
using Api.Domain.Entities.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

// Status online SourceVideo dari dua pengecekan:
// - SourceVideoHealthCheckService: cek file/URL/port setiap SystemConfig.SourceVideoHealthCheckIntervalMinutes;
// - laporan worker InferenceYolo (SucklingActivity/SyncStatus) saat stream kamera putus atau tersambung lagi.
// Keduanya memakai aturan yang sama: IsOnline diisi, dan bila status berbeda dari log terakhir ditulis
// SourceVideoStatusLog beserta notifikasi ke user farm kandangnya. Dibaca tanpa filter farm karena dipanggil
// worker/background (bukan user farm).
public sealed class SourceVideoStatusService
{
    public const string Online = "Online";
    public const string Inaccessible = "Inaccessible";
    private const string WorkerActor = "inference-worker";

    private readonly IServiceScopeFactory _scopeFactory;

    public SourceVideoStatusService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // Laporan worker; false bila SourceVideo aktif dengan kode itu tidak ada
    public async Task<bool> ReportAsync(string sourceVideoCode, bool isOnline, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sourceVideo = await db.SourceVideo.IgnoreQueryFilters().AsTracking().Include(x => x.Barn)
            .FirstOrDefaultAsync(x => x.IsActive && x.Code == sourceVideoCode, cancellationToken);
        if (sourceVideo == null) return false;

        await ApplyAsync(db, sourceVideo, isOnline ? Online : Inaccessible, DateTime.Now, WorkerActor, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static async Task ApplyAsync(
        ApplicationDbContext db,
        SourceVideo sourceVideo,
        string status,
        DateTime checkedAt,
        string actor,
        CancellationToken cancellationToken)
    {
        var lastStatus = await db.SourceVideoStatusLog.IgnoreQueryFilters()
            .Where(x => x.IsActive && x.FkIdSourceVideo == sourceVideo.Id)
            .OrderByDescending(x => x.LoggedAt ?? x.DateCreated)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        sourceVideo.IsOnline = status == Online;

        if (string.Equals(lastStatus, status, StringComparison.OrdinalIgnoreCase)) return;

        var statusLog = new SourceVideoStatusLog
        {
            Id = Guid.NewGuid(),
            FkIdSourceVideo = sourceVideo.Id,
            Code = Truncate($"CHK-{sourceVideo.Code}", 50),
            LoggedAt = checkedAt,
            Status = status,
            IsActive = true,
            CreatedBy = actor,
            ModifiedBy = actor
        };
        await db.SourceVideoStatusLog.AddAsync(statusLog, cancellationToken);
        await AddNotificationsForAllUsersAsync(db, $"SourceVideoStatusLog:{statusLog.Id}",
            $"SourceVideo {status}", $"SourceVideo {sourceVideo.Code} status changed to {status}.",
            status == Online ? "Success" : "Warning",
            $"/SourceVideoStatusLog/Details/{statusLog.Id}", actor, cancellationToken, sourceVideo.Barn?.FkFarmId);
    }

    public static async Task AddNotificationsForAllUsersAsync(
        ApplicationDbContext db,
        string code,
        string title,
        string content,
        string type,
        string returnLink,
        string actor,
        CancellationToken cancellationToken, Guid? farmId = null)
    {
        var usernames = await db.UserLogin.IgnoreQueryFilters()
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Username))
            .Where(x => farmId.HasValue
                ? db.UserFarm.IgnoreQueryFilters().Any(a => a.IsActive && a.FkUserId == x.Id && a.FkFarmId == farmId.Value)
                : db.UserFarm.IgnoreQueryFilters().Any(a => a.IsActive && a.FkUserId == x.Id) &&
                    !db.Farm.IgnoreQueryFilters().Any(f => f.IsActive &&
                        !db.UserFarm.IgnoreQueryFilters().Any(a => a.IsActive && a.FkUserId == x.Id && a.FkFarmId == f.Id)))
            .Select(x => x.Username)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var username in usernames)
        {
            await db.SystemNotification.AddAsync(new SystemNotification
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
                CreatedBy = actor,
                ModifiedBy = actor
            }, cancellationToken);
        }
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
