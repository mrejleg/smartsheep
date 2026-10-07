using Api.Domain.Entities.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Interfaces.Repositories
{
    public interface ISmartSheepRepositories
    {
        ApplicationDbContext Contexts { get; }
        IRepositoryPattern<SucklingActivity, Guid> SucklingActivities { get; }
        IRepositoryPattern<SucklingStatistic, Guid> SucklingStatistics { get; }
        IRepositoryPattern<SucklingEvent, Guid> SucklingEvents { get; }
        IRepositoryPattern<SourceVideoStatusLog, Guid> SourceVideoStatusLogs { get; }
        IRepositoryPattern<JobExecutionLog, Guid> JobExecutionLogs { get; }
        IRepositoryPattern<ReminderLog, Guid> ReminderLogs { get; }
    }
}
