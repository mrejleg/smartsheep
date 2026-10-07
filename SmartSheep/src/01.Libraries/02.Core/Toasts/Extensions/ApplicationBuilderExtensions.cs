using Microsoft.AspNetCore.Builder;
using Project.Core.Toasts.Middlewares;

namespace Project.Core.Toasts.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseNotyf(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<NotyfMiddleware>();
            return builder;
        }
    }
}
