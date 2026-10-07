using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Notification;
using Web.Domain.Models;
using Web.Domain.Models.Notifications;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public NotificationService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<RestApiResult<SystemNotification>> ReadNotificationAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SystemNotification>(_client, "Auth/ReadNotification/" + id);
        }
    }
}
