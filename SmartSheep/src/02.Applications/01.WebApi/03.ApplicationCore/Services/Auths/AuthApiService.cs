using Api.ApplicationCore.Interfaces.Auths;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Notifications;
using Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.Jwts;
using Project.Core.Extentions;
using Project.Core.Interfaces.Jwt;
using Api.Domain.Models.Auths;
using Api.Domain.Security;
using Api.Domain.Entities.Configs;
using Api.Domain.Entities.MobileApplications;
using Api.Domain.Models.MobileApps;

namespace Api.ApplicationCore.Services.Auths
{
    public class AuthApiService : IAuthApiService
    {
        private readonly IApiManagementRepositories _repository;
        private readonly ISystemNotificationRepositories _repositorySystemNotification;
        private readonly IJwtService _jwt;
        private readonly AppSetting _appSettings;

        public AuthApiService(IApiManagementRepositories repository, ISystemNotificationRepositories repositorySystemNotification, IJwtService jwt, AppSetting appSettings)
        {
            _repository = repository;
            _repositorySystemNotification = repositorySystemNotification;
            _jwt = jwt;
            _appSettings = appSettings;
        }

        public async Task<JwtResponse> GenerateJwtAsync(TokenJwtRequest tokenRequest)
        {
            var jwtResponse = new JwtResponse();

            if (GetClientSecret(tokenRequest.ClientId) != tokenRequest.ClientSecret)
            {
                return jwtResponse;
            }

            var clientApp = await _repository.AppClients.DataContext()
                .FirstOrDefaultAsync(x => x.ClientId == tokenRequest.ClientId);

            if (clientApp != null)
            {
                var jwtRequest = new JwtRequest
                {
                    JwtKey = _appSettings.JwtKey,
                    JwtIssuer = _appSettings.JwtIssuer,
                    ClientId = tokenRequest.ClientId,
                    ClientSecret = GetClientSecret(tokenRequest.ClientId),
                    JwtExpireIn = DateTime.Now.AddDays(int.Parse(_appSettings.JwtExpiresInDays)),
                    Username = tokenRequest.Username,
                    Email = tokenRequest.Email,
                    FullName = tokenRequest.FullName,
                    ApplicationId = clientApp.Id.ToString(),
                    ApplicationName = clientApp.Name
                };

                var roles = (tokenRequest.Roles ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                jwtRequest.Roles = string.Join(", ", roles.Distinct());
                jwtResponse = _jwt.GetJwt(jwtRequest);
            }

            return jwtResponse;
        }

        public async Task<AppUserResponseDto> AuthenticateAsync(LocalLoginRequest request)
        {
            var normalized = request.Username?.Trim();
            var user = await _repository.Contexts.UserLogin
                .Include(x => x.UserLoginRoles).ThenInclude(x => x.AppRole)
                .FirstOrDefaultAsync(x => x.IsActive && x.Username == normalized);
            if (user == null || !LocalPasswordHasher.Verify(request.Password, user.Password)) return null;

            user.LatestLogin = DateTime.Now;
            _repository.Contexts.UserLogin.Update(user);
            await _repository.Contexts.SaveChangesAsync();
            return await BuildLocalProfileAsync(user);
        }

        public async Task<AppUserResponseDto> GetLocalProfileAsync(string username)
        {
            var user = await _repository.Contexts.UserLogin
                .Include(x => x.UserLoginRoles).ThenInclude(x => x.AppRole)
                .FirstOrDefaultAsync(x => x.IsActive && x.Username == username);
            return user == null ? null : await BuildLocalProfileAsync(user);
        }

        public async Task<bool> UpdateUserImageAsync(string username, string userImage)
        {
            var user = await _repository.Contexts.UserLogin
                .FirstOrDefaultAsync(x => x.IsActive && x.Username == username);
            if (user == null)
            {
                return false;
            }

            user.UserImage = userImage;
            user.DateModified = DateTime.Now;
            _repository.Contexts.UserLogin.Update(user);
            await _repository.Contexts.SaveChangesAsync();
            return true;
        }

        public async Task<List<AppMenu>> MobileFavoriteMenuAsync(string username)
        {
            var user = await _repository.Contexts.UserLogin
                .Include(x => x.UserLoginRoles)
                .FirstOrDefaultAsync(x => x.IsActive && x.Username == username);
            if (user == null)
            {
                return new List<AppMenu>();
            }

            var roleIds = user.UserLoginRoles
                .Where(x => x.IsActive)
                .Select(x => x.FkRoleId)
                .ToList();
            var menuIds = await _repository.Contexts.AppMenuRole
                .AsNoTracking()
                .Where(x => x.IsActive && x.FkAppRoleId.HasValue && roleIds.Contains(x.FkAppRoleId.Value) && x.FkAppMenuId.HasValue)
                .Select(x => x.FkAppMenuId.Value)
                .Distinct()
                .ToListAsync();

            return await _repository.Contexts.MenuFavorite
                .AsNoTracking()
                .Include(x => x.AppMenu)
                .Where(x => x.IsActive && x.Email == user.Email && x.FkAppMenuId.HasValue &&
                    menuIds.Contains(x.FkAppMenuId.Value) && x.AppMenu != null &&
                    x.AppMenu.IsActive && x.AppMenu.IsMobile && !x.AppMenu.IsSection)
                .OrderBy(x => x.AppMenu.SequenceNumber)
                .Select(x => x.AppMenu)
                .ToListAsync();
        }

        public async Task<bool> MobilePostFavoriteMenuAsync(MenuFavoriteParamDto param)
        {
            if (param == null || string.IsNullOrEmpty(param.Email) || param.FkAppMenuIds == null ||
                param.FkAppMenuIds.Count > 4)
            {
                return false;
            }

            var user = await _repository.Contexts.UserLogin
                .Include(x => x.UserLoginRoles)
                .FirstOrDefaultAsync(x => x.IsActive && x.Email == param.Email);
            if (user == null)
            {
                return false;
            }

            var roleIds = user.UserLoginRoles
                .Where(x => x.IsActive)
                .Select(x => x.FkRoleId)
                .ToList();
            var allowedMenuIds = await _repository.Contexts.AppMenuRole
                .AsNoTracking()
                .Where(x => x.IsActive && x.FkAppRoleId.HasValue && roleIds.Contains(x.FkAppRoleId.Value) &&
                    x.FkAppMenuId.HasValue && x.AppMenu != null && x.AppMenu.IsActive &&
                    x.AppMenu.IsMobile && !x.AppMenu.IsSection)
                .Select(x => x.FkAppMenuId.Value)
                .Distinct()
                .ToListAsync();
            var selectedMenuIds = param.FkAppMenuIds
                .Where(x => x.HasValue && allowedMenuIds.Contains(x.Value))
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            var menuFavorites = await _repository.Contexts.MenuFavorite
                .Where(x => x.Email == param.Email)
                .ToListAsync();
            _repository.Contexts.MenuFavorite.RemoveRange(menuFavorites);

            foreach (var fkAppMenuId in selectedMenuIds)
            {
                await _repository.Contexts.MenuFavorite.AddAsync(new MenuFavorite
                {
                    Email = param.Email,
                    FkAppMenuId = fkAppMenuId,
                    IsActive = true
                });
            }

            await _repository.Contexts.SaveChangesAsync();
            return true;
        }

        private async Task<AppUserResponseDto> BuildLocalProfileAsync(Api.Domain.Entities.Auths.UserLogin user)
        {

            var roles = user.UserLoginRoles.Where(x => x.IsActive && x.AppRole?.IsActive == true)
                .Select(x => x.AppRole.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var roleIds = user.UserLoginRoles.Where(x => x.IsActive).Select(x => x.FkRoleId).ToList();
            var permissions = await _repository.Contexts.AppMenuRole.AsNoTracking()
                .Where(x => x.IsActive && x.FkAppRoleId.HasValue && roleIds.Contains(x.FkAppRoleId.Value))
                .ToListAsync();
            var menuIds = permissions.Where(x => x.FkAppMenuId.HasValue).Select(x => x.FkAppMenuId.Value).ToHashSet();
            var menus = await _repository.Contexts.AppMenu.AsNoTracking()
                .Where(x => x.IsActive && menuIds.Contains(x.Id)).ToListAsync();

            return new AppUserResponseDto
            {
                UserId = user.Id.ToString(), Username = user.Username, Email = user.Email,
                FullName = user.FullName, Photo = user.UserImage, PhoneNumber = user.HP, WaNumber = user.HP,
                Roles = string.Join(", ", roles), ListRoles = roles, IsSuccessResponse = true,
                // Login has no authenticated HTTP principal yet. Bind the query
                // explicitly to the user resolved by password authentication.
                Farms = await _repository.Contexts.Farm.IgnoreQueryFilters().AsNoTracking()
                    .Where(f => f.IsActive && _repository.Contexts.UserFarm.Any(a => a.IsActive && a.FkUserId == user.Id && a.FkFarmId == f.Id))
                    .OrderBy(f => f.Name)
                    .Select(f => new UserFarmProfileDto { Id = f.Id, Code = f.Code, Name = f.Name }).ToListAsync(),
                Menus = new NavMenuDto
                {
                    MenuParents = menus.Where(x => x.FkParentId == null).ToList(),
                    MenuChilds = menus.Where(x => x.FkParentId != null).ToList(),
                    MenuRoles = permissions
                }
            };
        }

        public async Task<bool> IsAuthorizeApiAsync(string scope, string clientId)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(scope))
            {
                return false;
            }

            if (ApiScopeAuthorizationCache.TryAuthorize(clientId, scope, out var cachedAuthorization))
            {
                return cachedAuthorization;
            }

            await ApiScopeAuthorizationCache.RefreshLock.WaitAsync();
            try
            {
                if (ApiScopeAuthorizationCache.TryAuthorize(clientId, scope, out cachedAuthorization))
                {
                    return cachedAuthorization;
                }

                var mappings = await _repository.AppScopeClients.DataContext()
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.AppClient != null && x.ScopeClient != null)
                    .Select(x => new
                    {
                        x.AppClient.ClientId,
                        Scope = x.ScopeClient.Name
                    })
                    .ToListAsync();

                var scopesByClient = mappings
                    .Where(x => !string.IsNullOrWhiteSpace(x.ClientId) && !string.IsNullOrWhiteSpace(x.Scope))
                    .GroupBy(x => x.ClientId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(x => x.Scope).ToHashSet(StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase);

                ApiScopeAuthorizationCache.Set(scopesByClient);
                return scopesByClient.TryGetValue(clientId, out var scopes) && scopes.Contains(scope);
            }
            finally
            {
                ApiScopeAuthorizationCache.RefreshLock.Release();
            }
        }

        public async Task WarmUpScopeAuthorizationAsync()
        {
            ApiScopeAuthorizationCache.Invalidate();
            await IsAuthorizeApiAsync("__scope_cache_warmup__", "__scope_cache_warmup__");
        }

        public async Task<List<SystemNotification>> SystemNotificationAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new List<SystemNotification>();
            }

            var normalizedUsername = username.Trim();
            return await _repositorySystemNotification.SystemNotifications.DataContext()
                .AsNoTracking()
                .Where(x => x.IsActive && x.Username == normalizedUsername)
                .OrderByDescending(x => x.DateCreated ?? x.DateModified)
                .ToListAsync();
        }

        public async Task<SystemNotification> ReadNotificationAsync(Guid id)
        {
            SystemNotification? result = null;

            var data = await _repositorySystemNotification.SystemNotifications.GetAsync(id);
            if (data != null && !data.IsRead)
            {
                data.IsRead = true;
                data.DateRead = DateTime.Now;
                await _repositorySystemNotification.SystemNotifications.UpdateAsync(data);
            }

            result = data;

            return result;
        }

        public string GetClientSecret(string textValue)
        {
            if (string.IsNullOrEmpty(textValue))
            {
                return textValue;
            }

            var textFormatEncrypt = TextExtentions.GetEncodeTextMd5(textValue, _appSettings.SecurityEncryptKeyMD5);
            return textFormatEncrypt;
        }
    }
}
