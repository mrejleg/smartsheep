using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Project.Core.Toasts.Abstractions;
using Project.Core.Toasts.Containers;
using Project.Core.Toasts.Middlewares;
using Project.Core.Toasts.Notyf;
using Project.Core.Toasts.Notyf.Models;
using Project.Core.Toasts.Services;

namespace Project.Core.Toasts.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static void AddFrameworkServices(this IServiceCollection services)
        {
            #region Framework Services
            //Check if a TempDataProvider is already registered.
            var tempDataProvider = services.FirstOrDefault(d => d.ServiceType == typeof(ITempDataProvider));
            if (tempDataProvider == null)
            {
                //Add a tempdata provider when one is not already registered
                services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();
            }
            //check if IHttpContextAccessor implementation is not registered. Add one if not.
            var httpContextAccessor = services.FirstOrDefault(d => d.ServiceType == typeof(IHttpContextAccessor));
            if (httpContextAccessor == null)
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }
            #endregion
        }

        public static void AddNotyf(this IServiceCollection services, Action<NotyfConfig> configure)
        {
            var configurationValue = new NotyfConfig();
            configure(configurationValue);
            var options = new NotyfEntity(configurationValue.DurationInSeconds, configurationValue.Position, configurationValue.IsDismissable);
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddFrameworkServices();

            //Add TempDataWrapper for accessing and adding values to tempdata.
            services.AddSingleton<ITempDataService, TempDataService>();
            // Add MessageContainerFactory for creating MessageContainer instance
            services.AddSingleton<IMessageContainerFactory, MessageContainerFactory>();
            //services.AddSingleton<IToastNotificationContainer<NotyfNotification>, TempDataToastNotificationContainer<NotyfNotification>>();
            //services.AddSingleton<IToastNotificationContainer<NotyfNotification>, InMemoryNotificationContainer<NotyfNotification>>();
            //Add the ToastNotification implementation
            services.AddScoped<INotyfService, NotyfService>();
            //Middleware
            services.AddScoped<NotyfMiddleware>();
            services.AddSingleton(options);

        }
    }
}
