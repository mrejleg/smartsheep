using Api.Domain.ApiManagements;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Interfaces.Repositories
{
    public interface IApiManagementRepositories
    {
        ApplicationDbContext Contexts { get; }
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();

        IRepositoryPattern<ScopeClient, Guid> ScopeClients { get; }
        IRepositoryPattern<AppClient, Guid> AppClients { get; }
        IRepositoryPattern<AppScopeClient, Guid> AppScopeClients { get; }
    }
}
