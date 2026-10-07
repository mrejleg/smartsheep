using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Api.Domain.Entities.Notifications;
using Api.Domain.Models;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Api.Services;

public sealed record FirebasePushResult(
    int SuccessCount,
    int FailureCount,
    string? FirstErrorCode = null,
    string? FirstErrorMessage = null);

public sealed class FirebasePushService
{
    private readonly Lazy<FirebaseMessaging> _messaging;
    private readonly ILogger<FirebasePushService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public FirebasePushService(
        AppSetting appSetting,
        ILogger<FirebasePushService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _messaging = new Lazy<FirebaseMessaging>(() =>
        {
            if (string.IsNullOrWhiteSpace(appSetting.FirebaseProjectId) ||
                string.IsNullOrWhiteSpace(appSetting.FirebasePrivateKey) ||
                string.IsNullOrWhiteSpace(appSetting.FirebaseClientEmail))
            {
                throw new InvalidOperationException("Firebase service-account credential is not configured.");
            }

            var credentialJson = JsonSerializer.Serialize(new
            {
                type = appSetting.FirebaseType,
                project_id = appSetting.FirebaseProjectId,
                private_key_id = appSetting.FirebasePrivateKeyId,
                private_key = appSetting.FirebasePrivateKey,
                client_email = appSetting.FirebaseClientEmail,
                client_id = appSetting.FirebaseClientId,
                auth_uri = appSetting.FirebaseAuthUri,
                token_uri = appSetting.FirebaseTokenUri,
                auth_provider_x509_cert_url = appSetting.FirebaseAuthProviderX509CertUrl,
                client_x509_cert_url = appSetting.FirebaseClientX509CertUrl,
                universe_domain = appSetting.FirebaseUniverseDomain
            });

            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(credentialJson),
                ProjectId = appSetting.FirebaseProjectId
            }, $"SmartSheep-{Guid.NewGuid():N}");

            return FirebaseMessaging.GetMessaging(app);
        });
    }

    public async Task<FirebasePushResult> SendAsync(
        IReadOnlyCollection<string> tokens,
        string title,
        string body,
        string? type,
        string? id,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var failureCount = 0;
        string? firstErrorCode = null;
        string? firstErrorMessage = null;
        var deadTokens = new List<string>();

        foreach (var tokenBatch in tokens.Distinct().Chunk(500))
        {
            var data = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(type)) data["type"] = type;
            if (!string.IsNullOrWhiteSpace(id)) data["id"] = id;
            // The system tray cannot render HTML, so it gets plain text; the
            // app's own popup reads the original markup to keep its styling.
            var plainBody = ToPlainText(body);
            if (plainBody != body) data["body_html"] = body;

            var response = await _messaging.Value.SendEachForMulticastAsync(
                new MulticastMessage
                {
                    Tokens = tokenBatch,
                    Notification = new Notification { Title = title, Body = plainBody },
                    Data = data,
                    // High priority wakes an idle device. The heads-up channel
                    // comes from the app manifest's FCM default. AndroidNotification
                    // is deliberately not set: the SDK serializes its unset fields,
                    // sending event_time 0001-01-01 and default_sound=false.
                    Android = new AndroidConfig { Priority = Priority.High }
                },
                cancellationToken);

            successCount += response.SuccessCount;
            failureCount += response.FailureCount;

            for (var index = 0; index < response.Responses.Count; index++)
            {
                var sendResponse = response.Responses[index];
                if (sendResponse.IsSuccess)
                {
                    continue;
                }

                var exception = sendResponse.Exception;
                // These codes mean the token itself is dead: the app was
                // reinstalled, the user logged in again and got a new token, or
                // the token belongs to another Firebase project. Retrying never
                // succeeds, so the token is retired below.
                if (exception?.MessagingErrorCode is MessagingErrorCode.Unregistered
                    or MessagingErrorCode.SenderIdMismatch)
                {
                    deadTokens.Add(tokenBatch[index]);
                }

                firstErrorCode ??= exception?.MessagingErrorCode?.ToString();
                firstErrorMessage ??= exception?.Message;
                _logger.LogWarning(
                    "Firebase rejected push notification for device {DeviceIndex}. MessagingErrorCode={MessagingErrorCode}; Message={Message}",
                    index + 1,
                    exception?.MessagingErrorCode,
                    exception?.Message);
            }
        }

        await DeactivateDeadTokensAsync(deadTokens, cancellationToken);

        return new FirebasePushResult(
            successCount,
            failureCount,
            firstErrorCode,
            firstErrorMessage);
    }

    private static string ToPlainText(string body)
    {
        if (string.IsNullOrEmpty(body) || !body.Contains('<')) return body;

        var text = Regex.Replace(body, @"<\s*br\s*/?>|</\s*(p|div|li|h[1-6])\s*>", "\n",
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);

        var lines = text.Split('\n')
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }

    private async Task DeactivateDeadTokensAsync(
        IReadOnlyCollection<string> deadTokens,
        CancellationToken cancellationToken)
    {
        if (deadTokens.Count == 0) return;

        try
        {
            // This service is a singleton, so it borrows a scoped context.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deactivated = await db.Set<UserDeviceToken>()
                .Where(x => x.IsActive && deadTokens.Contains(x.DeviceToken))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.DateModified, DateTime.Now), cancellationToken);

            _logger.LogInformation(
                "Deactivated {Count} device token(s) that Firebase reported as no longer valid.",
                deactivated);
        }
        catch (Exception ex)
        {
            // Cleanup must never turn a delivered notification into a failure.
            _logger.LogWarning(ex, "Unable to deactivate invalid device tokens.");
        }
    }
}
