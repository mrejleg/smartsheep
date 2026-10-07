using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.Notification;
using Web.Domain.Models;

namespace Web.ApplicationCore.Interfaces.Repositories
{
    public interface INotificationRepositories
    {
        AppSetting AppSettings { get; }
        IHttpContextAccessor HttpContextAccessors { get; }
        public INotificationService Notifications { get; }
    }
}
