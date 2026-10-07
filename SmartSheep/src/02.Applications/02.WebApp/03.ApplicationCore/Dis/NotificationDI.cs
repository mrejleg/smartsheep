using Microsoft.Extensions.DependencyInjection;
using Web.ApplicationCore.Interfaces.Notification;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.Notification;
using Web.ApplicationCore.Services.Repositories;

namespace Web.ApplicationCore.Dis
{
    public static class NotificationDI
    {

        public static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            services.AddTransient<INotificationService, NotificationService>();

            //Unit of Works
            services.AddTransient<INotificationRepositories, NotificationRepositories>();

            return services;
        }
    }

   
}
