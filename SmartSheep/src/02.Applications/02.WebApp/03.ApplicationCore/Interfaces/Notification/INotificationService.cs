using Project.Base.Models.RestApis;
using Web.Domain.Models.Notifications;

namespace Web.ApplicationCore.Interfaces.Notification
{
    public interface INotificationService
    {
        Task<RestApiResult<SystemNotification>> ReadNotificationAsync(Guid id);
    }
}
