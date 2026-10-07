using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.Auths;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.Auths;
using Web.Domain.Models;

namespace Web.ApplicationCore.Services.Repositories
{
    public class AuthRepositories : IAuthRepositories
    {
        public AppSetting AppSettings { get; private set; }
        public IHttpContextAccessor HttpContextAccessors { get; private set; }

        public IAuthService Auths { get; private set; }
        public IUserService Users { get; private set; }

        private readonly AppSetting _appSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthRepositories(AppSetting appSetting, IHttpContextAccessor httpContextAccessor)
        {
            _appSetting = appSetting;
            _httpContextAccessor = httpContextAccessor;

            AppSettings = _appSetting;
            HttpContextAccessors = new HttpContextAccessor();

            Users = new UserService(_httpContextAccessor, _appSetting);
            Auths = new AuthService(_httpContextAccessor, Users, _appSetting);
        }
    }
}
