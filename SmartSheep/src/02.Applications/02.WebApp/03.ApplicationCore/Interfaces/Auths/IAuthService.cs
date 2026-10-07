using Project.Base.Models.RestApis;
using Web.Domain.Models;
using Web.Domain.Models.Notifications;

namespace Web.ApplicationCore.Interfaces.Auths
{
    public interface IAuthService
    {
        bool IsAuthorizeSession();
        Task<bool> GetAccessMenuAsync(string controllerName, string accessType);
        Task<string> LoadMenuAsync(string controllerName, AppSetting appSettings);

        Task<RestApiResult<List<SystemNotification>>> SystemNotificationsAsync();
        void InvalidateSystemNotifications();
        Task<bool> GetAccessApplicationAsync();
    }
}
