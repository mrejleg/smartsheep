using Project.Core.Toasts.Abstractions;
using Project.Core.Toasts.Enums;

namespace Project.Core.Toasts.Notyf.Models
{
    public class NotyfNotification : Notification
    {
        public NotyfNotification(ToastNotificationType type, string message, int? durationInSeconds) : base(type, message, durationInSeconds)
        {

        }
        public string Icon { get; set; }
    }
}
