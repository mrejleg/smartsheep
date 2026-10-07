using Api.Domain.Entities.Notifications;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Interfaces.Repositories
{
    public interface ISystemNotificationRepositories
    {
        ApplicationDbContext Contexts { get; }
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();

        IRepositoryPattern<SystemNotification, Guid> SystemNotifications { get; }

    }
}
