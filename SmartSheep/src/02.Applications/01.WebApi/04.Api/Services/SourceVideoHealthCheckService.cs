using Api.Domain.Entities.Notifications;
using Api.Domain.Entities.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Text;

namespace Api.Services;

public sealed class SourceVideoHealthCheckService : BackgroundService
{
    private const int DefaultIntervalMinutes = 5;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SourceVideoHealthCheckService> _logger;

    public SourceVideoHealthCheckService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<SourceVideoHealthCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = DefaultIntervalMinutes;
            try
            {
                intervalMinutes = await CheckSourceVideosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SourceVideo health-check cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task<int> CheckSourceVideosAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var interval = await db.SystemConfig
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.DateModified ?? x.DateCreated)
            .Select(x => x.SourceVideoHealthCheckIntervalMinutes)
            .FirstOrDefaultAsync(cancellationToken);
        var intervalMinutes = interval > 0 ? interval : DefaultIntervalMinutes;
        var sourceVideos = await db.SourceVideo.AsTracking().Include(x => x.Barn)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var checkedAt = DateTime.Now;
        var onlineCount = 0;
        var inaccessibleCount = 0;

        foreach (var sourceVideo in sourceVideos)
        {
            var status = await GetStatusAsync(sourceVideo.SourceVideoUrl, cancellationToken);
            if (status == "Online") onlineCount++;
            else inaccessibleCount++;

            var lastStatus = await db.SourceVideoStatusLog
                .Where(x => x.IsActive && x.FkIdSourceVideo == sourceVideo.Id)
                .OrderByDescending(x => x.LoggedAt ?? x.DateCreated)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(cancellationToken);
            sourceVideo.IsOnline = status == "Online";

            if (string.Equals(lastStatus, status, StringComparison.OrdinalIgnoreCase)) continue;

            var statusLog = new SourceVideoStatusLog
            {
                Id = Guid.NewGuid(),
                FkIdSourceVideo = sourceVideo.Id,
                Code = Truncate($"CHK-{sourceVideo.Code}", 50),
                LoggedAt = checkedAt,
                Status = status,
                IsActive = true,
                CreatedBy = "sourceVideo-health-check",
                ModifiedBy = "sourceVideo-health-check"
            };
            await db.SourceVideoStatusLog.AddAsync(statusLog, cancellationToken);
            await AddNotificationsForAllUsersAsync(db, $"SourceVideoStatusLog:{statusLog.Id}",
                $"SourceVideo {status}", $"SourceVideo {sourceVideo.Code} status changed to {status}.",
                status == "Online" ? "Success" : "Warning",
                $"/SourceVideoStatusLog/Details/{statusLog.Id}", cancellationToken, sourceVideo.Barn?.FkFarmId);
        }

        var jobLog = new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            ExecutedAt = checkedAt,
            Code = "SOURCEVIDEO-HEALTH-CHECK",
            Status = "Succeeded",
            StatusNotes = $"Checked {sourceVideos.Count} sourceVideo(s): {onlineCount} online, {inaccessibleCount} inaccessible.",
            IsActive = true,
            CreatedBy = "sourceVideo-health-check",
            ModifiedBy = "sourceVideo-health-check"
        };
        await db.JobExecutionLog.AddAsync(jobLog, cancellationToken);
        await AddNotificationsForAllUsersAsync(db, $"JobExecutionLog:{jobLog.Id}",
            "Job Succeeded", jobLog.StatusNotes, "Success",
            $"/JobExecutionLog/Details/{jobLog.Id}", cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return intervalMinutes;
    }

    private async Task<string> GetStatusAsync(string sourceVideoUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(sourceVideoUrl, UriKind.Absolute, out var uri)) return "Inaccessible";

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, uri);
                using var response = await _httpClientFactory.CreateClient()
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                return response.IsSuccessStatusCode ? "Online" : "Inaccessible";
            }

            var port = uri.IsDefaultPort
                ? uri.Scheme.Equals("rtsp", StringComparison.OrdinalIgnoreCase) ? 554 : -1
                : uri.Port;
            if (port <= 0) return "Inaccessible";

            using var client = new TcpClient();
            await client.ConnectAsync(uri.Host, port, timeout.Token);

            if (!uri.Scheme.Equals("rtsp", StringComparison.OrdinalIgnoreCase)) return "Online";

            await using var stream = client.GetStream();
            var requestText = $"OPTIONS {uri} RTSP/1.0\r\nCSeq: 1\r\nUser-Agent: SmartSheep\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(requestText), timeout.Token);
            var responseBuffer = new byte[256];
            var bytesRead = await stream.ReadAsync(responseBuffer, timeout.Token);
            return bytesRead > 0 ? "Online" : "Inaccessible";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "Inaccessible";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SourceVideo health check failed for {SourceVideoUrl}.", sourceVideoUrl);
            return "Inaccessible";
        }
    }

    private static async Task AddNotificationsForAllUsersAsync(
        ApplicationDbContext db,
        string code,
        string title,
        string content,
        string type,
        string returnLink,
        CancellationToken cancellationToken, Guid? farmId = null)
    {
        var usernames = await db.UserLogin
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.Username))
            .Where(x => farmId.HasValue
                ? db.UserFarm.Any(a => a.IsActive && a.FkUserId == x.Id && a.FkFarmId == farmId.Value)
                : db.UserFarm.Any(a => a.IsActive && a.FkUserId == x.Id) &&
                    !db.Farm.Any(f => f.IsActive && !db.UserFarm.Any(a => a.IsActive && a.FkUserId == x.Id && a.FkFarmId == f.Id)))
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
                CreatedBy = "sourceVideo-health-check",
                ModifiedBy = "sourceVideo-health-check"
            }, cancellationToken);
        }
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
