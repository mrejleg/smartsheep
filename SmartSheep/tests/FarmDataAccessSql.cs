#:project ../src/02.Applications/01.WebApi/03.ApplicationCore/03.ApplicationCore.csproj
#:property PublishAot=false
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Api.Infrastructure.Data.DbContexts;
using Api.ApplicationCore.Services.Repositories;
using Api.ApplicationCore.Services.SmartSheep;
using Api.Domain.Models.Dashboards;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Local verification only. All test assignment changes are rolled back.
using var config = JsonDocument.Parse(File.ReadAllText(args[0]));
var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS: " + message); }
if (args.Contains("--before")) {
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT (SELECT COUNT(*) FROM dbo.Farm) Farms,
        (SELECT COUNT(*) FROM dbo.UserFarm) Assignments,
        (SELECT COUNT(*) FROM dbo.SucklingStatistic) [Statistics],
        (SELECT COUNT(*) FROM dbo.SucklingStatistic s WHERE 1=(
          SELECT COUNT(*) FROM dbo.Barn b JOIN dbo.Farm f ON f.Id=b.FkFarmId WHERE b.Code LIKE 'BRN-%' AND (
            s.Code='STAT-'+SUBSTRING(b.Code,5,50)+'-'+CONVERT(varchar(8),s.ActivityFrom,112)
            OR s.Code='STAT-'+f.Code+'-'+CONVERT(varchar(8),s.ActivityFrom,112)+'-'+RIGHT(b.Code,2)))) MatchedStatistics;
      """;
    await using var rows = await command.ExecuteReaderAsync();
    await rows.ReadAsync();
    for (var i=0; i<rows.FieldCount; i++) Console.WriteLine($"{rows.GetName(i)}: {rows.GetValue(i)}");
    Check(rows.GetInt32(2)==rows.GetInt32(3), "Every existing statistic has exactly one historical barn match");
    return;
}
var accessor = new HttpContextAccessor { HttpContext = null };
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options, accessor);
var allFarmCount = await db.Farm.CountAsync();
var admin = await db.UserLogin.SingleAsync(x=>x.Username=="SuperAdmin" && x.IsActive);
Check(await db.UserFarm.CountAsync(x=>x.FkUserId==admin.Id && x.IsActive)==allFarmCount, $"SuperAdmin assigned to all {allFarmCount} farms");
Check(!await db.SucklingStatistic.AnyAsync(x=>x.FkBarnId==null), "Existing statistics have explicit barn ownership");
var target = await db.Farm.FirstAsync(x=>x.IsActive);
await using var transaction = await db.Database.BeginTransactionAsync();
try {
    await db.UserFarm.Where(x=>x.FkUserId==admin.Id && x.FkFarmId!=target.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false));
    accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim("Username",Convert.ToBase64String(Encoding.UTF8.GetBytes(admin.Username))) },"Bearer")) };
    Check(await db.Farm.CountAsync()==1 && (await db.Farm.SingleAsync()).Id==target.Id,"SQL: request can only see its assigned farm");
    Check(!await db.Barn.AnyAsync(x=>x.FkFarmId!=target.Id),"SQL: barn dropdown excludes other farms");
    Check(!await db.SourceVideo.AnyAsync(x=>x.Barn.FkFarmId!=target.Id),"SQL: video sources exclude other farms");
    Check(!await db.SucklingStatistic.AnyAsync(x=>x.Barn.FkFarmId!=target.Id),"SQL: statistics exclude other farms");
    Check(!await db.ReminderLog.AnyAsync(x=>x.SucklingStatistic.Barn.FkFarmId!=target.Id),"SQL: reminders exclude other farms");
    Check(!await db.SystemNotification.AnyAsync(x=>x.FkFarmId!=null && x.FkFarmId!=target.Id),"SQL: notifications exclude other farms");
    Check(await db.JobExecutionLog.CountAsync()==0,"SQL: cross-farm job summaries are hidden");
    var reports = new SmartSheepReportService(new MasterDataRepositories(db),new SmartSheepRepositories(db));
    var dashboard = await reports.GetDashboardAsync(new DashboardFilterParam());
    Check(dashboard.TotalFarms==1,"SQL: dashboard aggregate respects assignment");
    Check(!await db.UserFarm.Where(x=>db.AssignedFarmIds.Contains(x.FkFarmId)).AnyAsync(x=>x.FkFarmId!=target.Id),"SQL: User Farms CRUD query respects assignment");
    await db.UserFarm.Where(x=>x.FkUserId==admin.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false));
    Check(await db.Farm.CountAsync()==0 && await db.SourceVideo.CountAsync()==0,"SQL: zero assignments fail closed");
} finally {
    accessor.HttpContext = null;
    await transaction.RollbackAsync();
}
Check(await db.UserFarm.CountAsync(x=>x.FkUserId==admin.Id && x.IsActive)==allFarmCount,"All test changes rolled back; SuperAdmin assignments preserved");
