using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Core.Interfaces.Jwt;
using Project.Core.Interfaces.Logging;
using Project.Core.Interfaces.Repositories;
using Project.Core.Interfaces.Repositories.Dappers;
using Project.Core.Jwt;
using Project.Core.Logging;
using Project.Core.Repositories;
using Project.Core.Repositories.Dappers;

namespace Project.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCores(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddTransient<IDapperRepository, DapperRepository>();
            services.AddTransient<IDapperRepositoryPattern, DapperRepositoryPattern>();

            services.AddScoped(typeof(IRepositoryPattern<,>), typeof(RepositoryPattern<,>));

            services.AddTransient<IJwtService, JwtService>();
            services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

            //Unit of Works
            services.AddTransient<IDapperRepo, DapperRepo>();

            return services;
        }
    }
}
