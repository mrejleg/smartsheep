using Api.Domain.ApiManagements;
using Api.Domain.Entities.Configs;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Interfaces.Repositories
{
    public interface IConfigRepositories
    {
        ApplicationDbContext Contexts { get; }
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();

        IRepositoryPattern<EmailTemplate, Guid> EmailTemplates { get; }
        IRepositoryPattern<AppClient, Guid> AppClients { get; }
        IRepositoryPattern<AppMenu, Guid> AppMenus { get; }
        IRepositoryPattern<AppMenuRole, Guid> AppMenuRoles { get; }
        IRepositoryPattern<AppRole, Guid> AppRoles { get; }
        IRepositoryPattern<SystemConfig, Guid> SystemConfigs { get; }
        IRepositoryPattern<ReminderTemplate, Guid> ReminderTemplates { get; }
        IRepositoryPattern<News, Guid> News { get; }
    }
}
