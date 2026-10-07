using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.Notification;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.Notification;
using Web.Domain.Models;

namespace Web.ApplicationCore.Services.Repositories
{
    public class NotificationRepositories : INotificationRepositories
    {
        public AppSetting AppSettings { get; private set; }
        public IHttpContextAccessor HttpContextAccessors { get; private set; }

        private readonly AppSetting _appSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public INotificationService Notifications { get; private set; }


        public NotificationRepositories(AppSetting appSetting, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _appSetting = appSetting;
            _httpContextAccessor = httpContextAccessor;

            AppSettings = _appSetting;
            HttpContextAccessors = new HttpContextAccessor();

            Notifications = new NotificationService(_appSetting);

        }
    }
}
