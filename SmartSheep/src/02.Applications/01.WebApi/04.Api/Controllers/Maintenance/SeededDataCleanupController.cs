using Api.Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Maintenance
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeededDataCleanupController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IHostEnvironment _hostEnvironment;

        public SeededDataCleanupController(ApplicationDbContext db, IHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        [HttpDelete("seeded-data")]
        public async Task<IActionResult> DeleteSeededData()
        {
            if (!_hostEnvironment.IsDevelopment())
            {
                return new Forbidden("Feature only available in development", new { message = "Run this endpoint only in development environment." }).ReturnResponse();
            }

            var deletedRows = new Dictionary<string, int>();

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                deletedRows["ReminderLog"] = await _db.ReminderLog.ExecuteDeleteAsync();
                deletedRows["SourceVideoStatusLog"] = await _db.SourceVideoStatusLog.ExecuteDeleteAsync();
                deletedRows["UserDeviceToken"] = await _db.UserDeviceToken.ExecuteDeleteAsync();
                deletedRows["MenuFavorite"] = await _db.MenuFavorite.ExecuteDeleteAsync();
                deletedRows["UserLoginRole"] = await _db.UserLoginRole.ExecuteDeleteAsync();
                deletedRows["JobExecutionLog"] = await _db.JobExecutionLog.ExecuteDeleteAsync();
                deletedRows["SucklingActivity"] = await _db.SucklingActivity.ExecuteDeleteAsync();
                deletedRows["SucklingStatistic"] = await _db.SucklingStatistic.ExecuteDeleteAsync();
                deletedRows["SourceVideo"] = await _db.SourceVideo.ExecuteDeleteAsync();
                deletedRows["Barn"] = await _db.Barn.ExecuteDeleteAsync();
                deletedRows["Farm"] = await _db.Farm.ExecuteDeleteAsync();
                deletedRows["Livestock"] = await _db.Livestock.ExecuteDeleteAsync();
                deletedRows["ReminderTemplate"] = await _db.ReminderTemplate.ExecuteDeleteAsync();
                deletedRows["News"] = await _db.News.ExecuteDeleteAsync();
                deletedRows["SystemNotification"] = await _db.SystemNotification.ExecuteDeleteAsync();

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new InternalServerError(ex.Message, null).ReturnResponse();
            }

            return new OK("Seeded data deleted successfully", deletedRows).ReturnResponse();
        }
    }
}
