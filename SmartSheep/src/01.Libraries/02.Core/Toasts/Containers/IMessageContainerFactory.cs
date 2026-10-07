using Project.Core.Toasts.Abstractions;

namespace Project.Core.Toasts.Containers
{
    public interface IMessageContainerFactory
    {
        IToastNotificationContainer<TMessage> Create<TMessage>() where TMessage : class;
    }
}
