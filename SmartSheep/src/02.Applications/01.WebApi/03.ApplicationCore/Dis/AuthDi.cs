using Api.ApplicationCore.Interfaces.Auths;
using Api.ApplicationCore.Services.Auths;
using Microsoft.Extensions.DependencyInjection;

namespace Api.ApplicationCore.Dis
{
    public static class AuthDi
    {
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            services.AddTransient<IAuthApiService, AuthApiService>();

            return services;
        }
    }
}
