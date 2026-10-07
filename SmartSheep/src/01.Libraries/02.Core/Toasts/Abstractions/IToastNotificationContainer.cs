namespace Project.Core.Toasts.Abstractions
{
    public interface IToastNotificationContainer<TNotification> where TNotification : class
    {
        void Add(TNotification notification);
        void RemoveAll();
        IList<TNotification> GetAll();
        IList<TNotification> ReadAll();
    }
}
