using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.ApiManagements;
using Api.Domain.Entities.Configs;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;
using Project.Core.Repositories;

namespace Api.ApplicationCore.Services.Repositories
{
    public class ConfigRepositories : IConfigRepositories
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public ApplicationDbContext Contexts { get; private set; }

        public IRepositoryPattern<EmailTemplate, Guid> EmailTemplates { get; private set; }
        public IRepositoryPattern<ScopeClient, Guid> ScopeClients { get; private set; }
        public IRepositoryPattern<AppClient, Guid> AppClients { get; private set; }
        public IRepositoryPattern<AppScopeClient, Guid> AppScopeClients { get; private set; }
        public IRepositoryPattern<AppRole, Guid> AppRoles { get; private set; }
        public IRepositoryPattern<AppMenu, Guid> AppMenus { get; private set; }
        public IRepositoryPattern<AppMenuRole, Guid> AppMenuRoles { get; private set; }
        public IRepositoryPattern<SystemConfig, Guid> SystemConfigs { get; private set; }
        public IRepositoryPattern<ReminderTemplate, Guid> ReminderTemplates { get; private set; }
        public IRepositoryPattern<News, Guid> News { get; private set; }

        public ConfigRepositories(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;

            Contexts = _applicationDbContext;

            EmailTemplates = new RepositoryPattern<EmailTemplate, Guid>(_applicationDbContext);
            ScopeClients = new RepositoryPattern<ScopeClient, Guid>(_applicationDbContext);
            AppClients = new RepositoryPattern<AppClient, Guid>(_applicationDbContext);
            AppScopeClients = new RepositoryPattern<AppScopeClient, Guid>(_applicationDbContext);
            AppRoles = new RepositoryPattern<AppRole, Guid>(_applicationDbContext);
            AppMenus = new RepositoryPattern<AppMenu, Guid>(_applicationDbContext);
            AppMenuRoles = new RepositoryPattern<AppMenuRole, Guid>(_applicationDbContext);
            SystemConfigs = new RepositoryPattern<SystemConfig, Guid>(_applicationDbContext);
            ReminderTemplates = new RepositoryPattern<ReminderTemplate, Guid>(_applicationDbContext);
            News = new RepositoryPattern<News, Guid>(_applicationDbContext);
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _applicationDbContext.Database.BeginTransaction();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _applicationDbContext.Database.BeginTransactionAsync();
        }
    }
}
