using Api.Infrastructure.Data.DbContexts.GenerateViews;
using Project.Core.Interfaces.Logging;

namespace Api.Infrastructure.Data.DbContexts
{
    public class ApplicationDbContextView
    {
        public static async Task ApplicationDbContextViewAsync(ApplicationDbContext applicationDbContext, IAppLogger<ApplicationDbContext> appLogger, int? retry = 0)
        {
            int retryForAvailability = retry ?? 0;
            try
            {
                //applicationDbContext.InjectViewSqlServer(applicationDbContext.Database, SqlViews.EmployeePositionSql(), "EmployeePositionView");
            }
            catch (Exception ex)
            {
                if (retryForAvailability < 10)
                {
                    retryForAvailability++;
                    appLogger.LogError(ex.Message.ToString());

                    await ApplicationDbContextViewAsync(applicationDbContext, appLogger, retryForAvailability);
                    return;
                }
                throw;
            }
        }
    }
}
