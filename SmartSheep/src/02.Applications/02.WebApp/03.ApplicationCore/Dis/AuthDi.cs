using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.ApplicationCore.Interfaces.Auths;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.Auths;
using Web.ApplicationCore.Services.Repositories;

namespace Web.ApplicationCore.Dis
{
    public static class AuthDi
    {
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            //Auths
            // These services carry the current request's session/token headers.
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            //Unit of Works
            services.AddTransient<IAuthRepositories, AuthRepositories>();

            return services;
        }
    }
}
