using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;
using Project.Core.Repositories;

namespace Api.ApplicationCore.Services.Repositories
{
    public class SystemNotificationRepositories : ISystemNotificationRepositories
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public ApplicationDbContext Contexts { get; private set; }

        //Add repositories in here
        public IRepositoryPattern<SystemNotification, Guid> SystemNotifications { get; private set; }


        public SystemNotificationRepositories(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;

            Contexts = _applicationDbContext;
            SystemNotifications = new RepositoryPattern<SystemNotification, Guid>(_applicationDbContext);
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
