using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.ApiManagements;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;
using Project.Core.Repositories;

namespace Api.ApplicationCore.Services.Repositories
{
    public class ApiManagementRepositories : IApiManagementRepositories
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public ApplicationDbContext Contexts { get; private set; }

        public IRepositoryPattern<ScopeClient, Guid> ScopeClients { get; private set; }
        public IRepositoryPattern<AppClient, Guid> AppClients { get; private set; }
        public IRepositoryPattern<AppScopeClient, Guid> AppScopeClients { get; private set; }

        public ApiManagementRepositories(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;

            Contexts = _applicationDbContext;

            ScopeClients = new RepositoryPattern<ScopeClient, Guid>(_applicationDbContext);
            AppClients = new RepositoryPattern<AppClient, Guid>(_applicationDbContext);
            AppScopeClients = new RepositoryPattern<AppScopeClient, Guid>(_applicationDbContext);
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
