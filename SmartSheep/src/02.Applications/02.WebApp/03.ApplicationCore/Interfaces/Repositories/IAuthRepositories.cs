using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.Auths;
using Web.Domain.Models;

namespace Web.ApplicationCore.Interfaces.Repositories
{
    public interface IAuthRepositories
    {
        AppSetting AppSettings { get; }
        IHttpContextAccessor HttpContextAccessors { get; }
        IAuthService Auths { get; }
        IUserService Users { get; }
    }
}
