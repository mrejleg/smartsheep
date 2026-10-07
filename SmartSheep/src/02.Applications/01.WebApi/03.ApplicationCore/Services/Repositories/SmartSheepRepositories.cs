using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Project.Core.Interfaces.Repositories;
using Project.Core.Repositories;

namespace Api.ApplicationCore.Services.Repositories
{
    public class SmartSheepRepositories : ISmartSheepRepositories
    {
        public ApplicationDbContext Contexts { get; private set; }
        public IRepositoryPattern<SucklingActivity, Guid> SucklingActivities { get; private set; }
        public IRepositoryPattern<SucklingStatistic, Guid> SucklingStatistics { get; private set; }
        public IRepositoryPattern<SucklingEvent, Guid> SucklingEvents { get; private set; }
        public IRepositoryPattern<SourceVideoStatusLog, Guid> SourceVideoStatusLogs { get; private set; }
        public IRepositoryPattern<JobExecutionLog, Guid> JobExecutionLogs { get; private set; }
        public IRepositoryPattern<ReminderLog, Guid> ReminderLogs { get; private set; }

        public SmartSheepRepositories(ApplicationDbContext applicationDbContext)
        {
            Contexts = applicationDbContext;
            SucklingActivities = new RepositoryPattern<SucklingActivity, Guid>(applicationDbContext);
            SucklingStatistics = new RepositoryPattern<SucklingStatistic, Guid>(applicationDbContext);
            SucklingEvents = new RepositoryPattern<SucklingEvent, Guid>(applicationDbContext);
            SourceVideoStatusLogs = new RepositoryPattern<SourceVideoStatusLog, Guid>(applicationDbContext);
            JobExecutionLogs = new RepositoryPattern<JobExecutionLog, Guid>(applicationDbContext);
            ReminderLogs = new RepositoryPattern<ReminderLog, Guid>(applicationDbContext);
        }
    }
}
