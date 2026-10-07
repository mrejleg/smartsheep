using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.Firebases;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Api.Controllers;

public class FirebaseNotificationController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    private readonly FirebasePushService _firebase;

    public FirebaseNotificationController(ApplicationDbContext db, FirebasePushService firebase)
    {
        _db = db;
        _firebase = firebase;
    }

    [HttpPost]
    [AuthorizeFilter(Scope = "FirebaseNotification.Add")]
    public async Task<IActionResult> PostAsync(
        [FromBody] NotificationRequestDto param,
        CancellationToken cancellationToken)
    {
        if (param == null || string.IsNullOrWhiteSpace(param.Title) || string.IsNullOrWhiteSpace(param.Message) ||
            param.Username == null || param.Username.Length == 0)
        {
            return new BadRequest("Title, Message and Username are required", param).ReturnResponse();
        }

        var usernames = param.Username
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tokens = await _db.Set<UserDeviceToken>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.UserLogin != null &&
                x.UserLogin.IsActive && usernames.Contains(x.UserLogin.Username))
            .Select(x => x.DeviceToken)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return new NotFound("No active device is registered for the requested username", new { param.Username })
                .ReturnResponse();
        }

        var result = await _firebase.SendAsync(
            tokens, param.Title, param.Message, param.Type, param.Id, cancellationToken);

        if (result.SuccessCount == 0)
        {
            return new Conflict("Failed send notification", new
            {
                param.Username,
                result.SuccessCount,
                result.FailureCount
            }).ReturnResponse();
        }

        return new OK("Success send notification", new
        {
            param.Username,
            result.SuccessCount,
            result.FailureCount
        }).ReturnResponse();
    }

    [HttpPost("RegisterDevice")]
    public async Task<IActionResult> RegisterDeviceAsync(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = GetAuthenticatedUsername();
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        var user = await _db.UserLogin
            .FirstOrDefaultAsync(x => x.IsActive && x.Username == username, cancellationToken);
        if (user == null) return NotFound(new { Message = "Authenticated user was not found." });

        var deviceToken = request.DeviceToken.Trim();
        var registration = await _db.Set<UserDeviceToken>()
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken, cancellationToken);

        if (registration == null)
        {
            registration = new UserDeviceToken
            {
                Id = Guid.NewGuid(),
                FkUserId = user.Id,
                DeviceToken = deviceToken,
                Platform = request.Platform?.Trim(),
                DeviceName = request.DeviceName?.Trim(),
                IsActive = true
            };
            await _db.Set<UserDeviceToken>().AddAsync(registration, cancellationToken);
        }
        else
        {
            registration.FkUserId = user.Id;
            registration.Platform = request.Platform?.Trim();
            registration.DeviceName = request.DeviceName?.Trim();
            registration.IsActive = true;
            _db.Set<UserDeviceToken>().Update(registration);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = "Device registered successfully." });
    }

    [HttpDelete("UnregisterDevice")]
    public async Task<IActionResult> UnregisterDeviceAsync(
        [FromBody] UnregisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var username = GetAuthenticatedUsername();
        if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

        var registration = await _db.Set<UserDeviceToken>()
            .Include(x => x.UserLogin)
            .FirstOrDefaultAsync(x => x.DeviceToken == request.DeviceToken.Trim() &&
                x.UserLogin != null && x.UserLogin.Username == username, cancellationToken);

        if (registration == null) return NoContent();

        registration.IsActive = false;
        _db.Set<UserDeviceToken>().Update(registration);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? GetAuthenticatedUsername()
    {
        var encoded = User.FindFirst("Username")?.Value;
        if (string.IsNullOrWhiteSpace(encoded)) return null;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public sealed class RegisterDeviceRequest
{
    [Required, StringLength(512, MinimumLength = 1)]
    public string DeviceToken { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Platform { get; set; }

    [StringLength(100)]
    public string? DeviceName { get; set; }
}

public sealed class UnregisterDeviceRequest
{
    [Required, StringLength(512, MinimumLength = 1)]
    public string DeviceToken { get; set; } = string.Empty;
}
