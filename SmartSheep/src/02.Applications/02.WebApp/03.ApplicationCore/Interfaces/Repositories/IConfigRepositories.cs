using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.ApiManagements;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;

namespace Web.ApplicationCore.Interfaces.Repositories
{
    public interface IConfigRepositories
    {
        AppSetting AppSettings { get; }
        IHttpContextAccessor HttpContextAccessors { get; }
        public IAppClientService AppClients { get; }
        public IAppScopeClientService AppScopeClients { get; }
        public IScopeClientService ScopeClients { get; }
        public IAppMenuService AppMenus { get; }
        public IAppMenuRoleService AppMenuRoles { get; }
        public IAppRoleService AppRoles { get; }
        public IEmailTemplateService EmailTemplates { get; }
        public ISystemConfigService SystemConfigs { get; }
        public IReminderTemplateService ReminderTemplates { get; }
        public INewsService News { get; }
    }
}
