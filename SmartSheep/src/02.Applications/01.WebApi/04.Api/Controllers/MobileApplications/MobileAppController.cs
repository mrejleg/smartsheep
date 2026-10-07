using Api.ApplicationCore.Interfaces.Auths;
using Api.ApplicationCore.Interfaces.Configs;
using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Api.Domain.Models;
using Api.Domain.Models.Auths;
using Api.Domain.Models.MobileApps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.Jwts;
using Project.Core.Files;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.MobileApplications
{
    public class MobileAppController : BaseApiController
    {
        private readonly IAuthApiService _authService;
        private readonly INewsService _newsService;
        private readonly AppSetting _appSetting;
        private readonly ApplicationDbContext _db;

        public MobileAppController(IAuthApiService authService, INewsService newsService, AppSetting appSetting,
            ApplicationDbContext db)
        {
            _authService = authService;
            _newsService = newsService;
            _appSetting = appSetting;
            _db = db;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult> LoginAsync([FromBody] MobileLoginRequest param)
        {
            if (param == null)
            {
                return new BadRequest("Parameter is null", param).ReturnResponse();
            }

            if (!ModelState.IsValid)
            {
                return new BadRequest("Client Id, Client Secret, Username and Password are required", param).ReturnResponse();
            }

            var profile = await _authService.AuthenticateAsync(new LocalLoginRequest
            {
                Username = param.Username,
                Password = param.Password
            });
            if (profile == null)
            {
                return new Unauthorized("The username or password is incorrect", new { param.Username }).ReturnResponse();
            }

            var token = await _authService.GenerateJwtAsync(new TokenJwtRequest
            {
                ClientId = param.ClientId,
                ClientSecret = param.ClientSecret,
                Username = profile.Username,
                Email = profile.Email,
                FullName = profile.FullName,
                Roles = profile.Roles
            });
            if (string.IsNullOrEmpty(token.Token))
            {
                return new Unauthorized("The client id or client secret is incorrect", new { param.ClientId }).ReturnResponse();
            }

            if (!string.IsNullOrWhiteSpace(param.DeviceToken))
            {
                await RegisterDeviceAsync(profile.Username, param);
            }

            return new OK("Success login", new { Token = token, Profile = profile }).ReturnResponse();
        }

        private async Task RegisterDeviceAsync(string username, MobileLoginRequest param)
        {
            var userId = await _db.UserLogin
                .Where(x => x.IsActive && x.Username == username)
                .Select(x => x.Id)
                .FirstAsync();

            var deviceToken = param.DeviceToken!.Trim();
            var registration = await _db.Set<UserDeviceToken>()
                .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);

            if (registration == null)
            {
                await _db.Set<UserDeviceToken>().AddAsync(new UserDeviceToken
                {
                    Id = Guid.NewGuid(),
                    FkUserId = userId,
                    DeviceToken = deviceToken,
                    Platform = param.Platform?.Trim(),
                    DeviceName = param.DeviceName?.Trim(),
                    IsActive = true
                });
            }
            else
            {
                registration.FkUserId = userId;
                registration.Platform = param.Platform?.Trim();
                registration.DeviceName = param.DeviceName?.Trim();
                registration.IsActive = true;
            }

            await _db.SaveChangesAsync();
        }

        [HttpGet("Profile")]
        public async Task<IActionResult> ProfileAsync([FromQuery] string username)
        {
            if (!string.Equals(username, _db.CurrentUsername, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(_db.CurrentUsername)) return Forbid();
            if (string.IsNullOrEmpty(username))
            {
                return new BadRequest("Username is required", new { username }).ReturnResponse();
            }

            var data = await _authService.GetLocalProfileAsync(username);
            if (data == null)
            {
                return new NotFound("Data not found : " + username, new { username }).ReturnResponse();
            }

            return new OK("Success get data by " + username, data).ReturnResponse();
        }

        [HttpGet("AccessMenu")]
        public async Task<IActionResult> AccessMenuAsync([FromQuery] string username)
        {
            if (!string.Equals(username, _db.CurrentUsername, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(_db.CurrentUsername)) return Forbid();
            if (string.IsNullOrEmpty(username))
            {
                return new BadRequest("Username is required", new { username }).ReturnResponse();
            }

            var profile = await _authService.GetLocalProfileAsync(username);
            if (profile == null)
            {
                return new NotFound("Data not found : " + username, new { username }).ReturnResponse();
            }

            var mobileMenuIds = profile.Menus.MenuParents
                .Concat(profile.Menus.MenuChilds)
                .Where(x => x.IsMobile)
                .Select(x => x.Id)
                .ToHashSet();
            profile.Menus = new NavMenuDto
            {
                MenuParents = profile.Menus.MenuParents.Where(x => x.IsMobile).ToList(),
                MenuChilds = profile.Menus.MenuChilds.Where(x => x.IsMobile).ToList(),
                MenuRoles = profile.Menus.MenuRoles
                    .Where(x => x.FkAppMenuId.HasValue && mobileMenuIds.Contains(x.FkAppMenuId.Value))
                    .ToList()
            };

            return new OK("Success get data by " + username, profile.Menus).ReturnResponse();
        }

        [HttpGet("MenuFavorite")]
        public async Task<IActionResult> MenuFavoriteAsync([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new BadRequest("Username is required", new { username }).ReturnResponse();
            }

            var data = await _authService.MobileFavoriteMenuAsync(username);
            return new OK("Success get data by " + username, data).ReturnResponse();
        }

        [HttpPost("MobilePostFavoriteMenu")]
        public async Task<IActionResult> MobilePostFavoriteMenuAsync([FromBody] MenuFavoriteParamDto param)
        {
            if (param == null || string.IsNullOrEmpty(param.Email) || param.FkAppMenuIds == null)
            {
                return new BadRequest("Email and List Menu Id are required", param).ReturnResponse();
            }

            var isSuccess = await _authService.MobilePostFavoriteMenuAsync(param);
            if (!isSuccess)
            {
                return new NotFound("Data not found : " + param.Email, new { param.Email }).ReturnResponse();
            }

            return new OK("Success update favorite menu", isSuccess).ReturnResponse();
        }

        [AllowAnonymous]
        [HttpGet("ProfileImage")]
        public async Task<IActionResult> ProfileImageAsync([FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new BadRequest("Username is required", new { username }).ReturnResponse();
            }

            var profile = await _authService.GetLocalProfileAsync(username);
            if (profile == null || string.IsNullOrEmpty(profile.Photo))
            {
                return new NotFound("Profile image not found : " + username, new { username }).ReturnResponse();
            }

            var filePath = Path.Combine(_appSetting.FileUrl, "images", "uploads", Path.GetFileName(profile.Photo));
            if (!System.IO.File.Exists(filePath))
            {
                return new NotFound("Profile image not found : " + username, new { username }).ReturnResponse();
            }

            var image = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(image, GetContentType(Path.GetExtension(filePath)), Path.GetFileName(filePath));
        }

        [HttpPost("ProfileImage")]
        public async Task<IActionResult> ProfileImageAsync([FromBody] AppUserProfileDto data)
        {
            if (data == null || string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.PhotoBase64) ||
                string.IsNullOrEmpty(data.FileExtention))
            {
                return new BadRequest("Username, Photo Base64 and File Extension are required", data).ReturnResponse();
            }

            var fileExtension = data.FileExtention.ToLowerInvariant();
            var extensions = _appSetting.FileAllowedFileExtensions.ToLowerInvariant()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
            if (!extensions.Contains(fileExtension) || !imageExtensions.Contains(fileExtension))
            {
                return new BadRequest("Photo extension is not allowed : " + fileExtension, new { fileExtension }).ReturnResponse();
            }

            byte[] image;
            try
            {
                image = Convert.FromBase64String(data.PhotoBase64);
            }
            catch (FormatException)
            {
                return new BadRequest("Photo Base64 is invalid", new { data.Username }).ReturnResponse();
            }

            var maximumSize = long.Parse(_appSetting.FileMaximumFileSize);
            if (SizeExtensions.ConvertBytesToMegabytes(image.Length) > maximumSize)
            {
                return new BadRequest("Photo too large, maximum size : " + maximumSize + " MB", new { data.Username }).ReturnResponse();
            }

            var directory = Path.Combine(_appSetting.FileUrl, "images", "uploads");
            Directory.CreateDirectory(directory);
            var fileName = Guid.NewGuid() + fileExtension;
            await System.IO.File.WriteAllBytesAsync(Path.Combine(directory, fileName), image);

            var isSuccess = await _authService.UpdateUserImageAsync(data.Username, fileName);
            if (!isSuccess)
            {
                System.IO.File.Delete(Path.Combine(directory, fileName));
                return new NotFound("Data not found : " + data.Username, new { data.Username }).ReturnResponse();
            }

            return new OK("Success update profile image", new { FileName = fileName }).ReturnResponse();
        }

        [AllowAnonymous]
        [HttpGet("News")]
        public async Task<IActionResult> NewsAsync()
        {
            var currentDate = DateTime.Now;
            var data = await _newsService.GetAll().AsNoTracking()
                .Where(x => x.IsActive && (!x.StartDate.HasValue || x.StartDate <= currentDate) &&
                    (!x.EndDate.HasValue || x.EndDate >= currentDate))
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            return new OK("Success get all data", data).ReturnResponse();
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".jpeg" => "image/jpeg",
                _ => "image/jpeg"
            };
        }
    }
}
