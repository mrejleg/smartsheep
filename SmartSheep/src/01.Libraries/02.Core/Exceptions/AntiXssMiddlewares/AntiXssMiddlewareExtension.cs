using Microsoft.AspNetCore.Builder;

namespace Project.Core.Exceptions.AntiXssMiddlewares
{
    public static class AntiXssMiddlewareExtension
    {
        public static IApplicationBuilder UseAntiXssMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiXssMiddleware>();
        }
    }
}
