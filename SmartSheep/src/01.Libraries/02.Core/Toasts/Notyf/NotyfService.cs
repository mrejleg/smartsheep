using Project.Core.Toasts.Abstractions;
using Project.Core.Toasts.Containers;
using Project.Core.Toasts.Enums;
using Project.Core.Toasts.Notyf.Models;

namespace Project.Core.Toasts.Notyf
{
    public class NotyfService : INotyfService
    {
        protected IToastNotificationContainer<NotyfNotification> MessageContainer;

        public NotyfService(IMessageContainerFactory messageContainerFactory)
        {
            MessageContainer = messageContainerFactory.Create<NotyfNotification>();
        }
        public void Custom(string message, int? durationInSeconds = null, string backgroundColor = "black", string iconClassName = "home")
        {
            var toastMessage = new NotyfNotification(ToastNotificationType.Custom, message, durationInSeconds);
            toastMessage.Icon = iconClassName;
            toastMessage.BackgroundColor = backgroundColor;
            MessageContainer.Add(toastMessage);
        }

        public void Error(string message, int? durationInSeconds = null)
        {
            var toastMessage = new NotyfNotification(ToastNotificationType.Error, message, durationInSeconds);
            MessageContainer.Add(toastMessage);
        }

        public IEnumerable<NotyfNotification> GetNotifications()
        {
            return MessageContainer.GetAll();
        }

        public void Information(string message, int? durationInSeconds = null)
        {
            var toastMessage = new NotyfNotification(ToastNotificationType.Information, message, durationInSeconds);
            MessageContainer.Add(toastMessage);
        }

        public IEnumerable<NotyfNotification> ReadAllNotifications()
        {
            return MessageContainer.ReadAll();
        }

        public void RemoveAll()
        {
            MessageContainer.RemoveAll();
        }

        public void Success(string message, int? durationInSeconds = null)
        {
            var toastMessage = new NotyfNotification(ToastNotificationType.Success, message, durationInSeconds);
            MessageContainer.Add(toastMessage);
        }

        public void Warning(string message, int? durationInSeconds = null)
        {
            var toastMessage = new NotyfNotification(ToastNotificationType.Warning, message, durationInSeconds);
            MessageContainer.Add(toastMessage);
        }
    }
}
