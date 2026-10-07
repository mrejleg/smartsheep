using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.ApiManagements;
using Web.ApplicationCore.Interfaces.Configs;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.ApiManagements;
using Web.ApplicationCore.Services.Configs;
using Web.Domain.Models;

namespace Web.ApplicationCore.Services.Repositories
{
    public class ConfigsRepositories : IConfigRepositories
    {
        public AppSetting AppSettings { get; private set; }
        public IHttpContextAccessor HttpContextAccessors { get; private set; }

        private readonly AppSetting _appSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IAppClientService AppClients { get; private set; }
        public IAppScopeClientService AppScopeClients { get; private set; }
        public IScopeClientService ScopeClients { get; private set; }
        public IAppMenuService AppMenus { get; private set; }
        public IAppMenuRoleService AppMenuRoles { get; private set; }
        public IAppRoleService AppRoles { get; private set; }
        public IEmailTemplateService EmailTemplates { get; private set; }
        public ISystemConfigService SystemConfigs { get; private set; }
        public IReminderTemplateService ReminderTemplates { get; private set; }
        public INewsService News { get; private set; }

        public ConfigsRepositories(AppSetting appSetting, IHttpContextAccessor httpContextAccessor)
        {
            _appSetting = appSetting;
            _httpContextAccessor = httpContextAccessor;

            AppSettings = _appSetting;
            HttpContextAccessors = new HttpContextAccessor();

            AppClients = new AppClientService(_appSetting);
            AppScopeClients = new AppScopeClientService(_appSetting);
            ScopeClients = new ScopeClientService(_appSetting);
            AppMenus = new AppMenuService(_appSetting);
            AppRoles = new AppRoleService(_appSetting);
            AppMenuRoles = new AppMenuRoleService(_appSetting);
            EmailTemplates = new EmailTemplateService(_appSetting);
            SystemConfigs = new SystemConfigService(_appSetting);
            ReminderTemplates = new ReminderTemplateService(_appSetting);
            News = new NewsService(_appSetting);
        }
    }
}
