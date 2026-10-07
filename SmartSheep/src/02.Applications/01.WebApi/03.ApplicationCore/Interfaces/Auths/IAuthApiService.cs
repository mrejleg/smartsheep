using Api.Domain.Entities.Notifications;
using Project.Base.Models.Jwts;
using Api.Domain.Models.Auths;
using Api.Domain.Entities.Configs;
using Api.Domain.Models.MobileApps;

namespace Api.ApplicationCore.Interfaces.Auths
{
    public interface IAuthApiService
    {
        Task<JwtResponse> GenerateJwtAsync(TokenJwtRequest tokenRequest);
        Task<AppUserResponseDto> AuthenticateAsync(LocalLoginRequest request);
        Task<AppUserResponseDto> GetLocalProfileAsync(string username);
        Task<bool> UpdateUserImageAsync(string username, string userImage);
        Task<List<AppMenu>> MobileFavoriteMenuAsync(string username);
        Task<bool> MobilePostFavoriteMenuAsync(MenuFavoriteParamDto param);
        Task WarmUpScopeAuthorizationAsync();
        Task<bool> IsAuthorizeApiAsync(string scope, string fkClientAppId);
        Task<List<SystemNotification>> SystemNotificationAsync(string username);
        Task<SystemNotification> ReadNotificationAsync(Guid id);
        string GetClientSecret(string textValue);
    }
}
