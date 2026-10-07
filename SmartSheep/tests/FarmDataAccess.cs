#:project ../src/02.Applications/01.WebApi/03.ApplicationCore/03.ApplicationCore.csproj
#:package Microsoft.EntityFrameworkCore.InMemory@10.0.5
#:property PublishAot=false
using Api.Domain.Entities.Auths;
using Api.Domain.Entities.MasterDatas;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Entities.Notifications;
using Api.Domain.Entities.Configs;
using Api.Domain.Models.Dashboards;
using Api.Domain.Models.Reports;
using Api.ApplicationCore.Services.Repositories;
using Api.ApplicationCore.Services.SmartSheep;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS: " + message); }
HttpContextAccessor Request(string? user, bool authenticated = true) => new() { HttpContext = new DefaultHttpContext {
    User = new ClaimsPrincipal(new ClaimsIdentity(user == null ? [] : new[] { new Claim("Username", Convert.ToBase64String(Encoding.UTF8.GetBytes(user))) }, authenticated ? "Bearer" : null)) } };
var a = new Farm { Id=Guid.NewGuid(),Code="A",Name="Farm A",IsActive=true };
var b = new Farm { Id=Guid.NewGuid(),Code="B",Name="Farm B",IsActive=true };
var userA = new UserLogin { Id=Guid.NewGuid(),Username="a",FullName="A",Password="test",IsActive=true };
var userB = new UserLogin { Id=Guid.NewGuid(),Username="b",FullName="B",Password="test",IsActive=true };
var admin = new UserLogin { Id=Guid.NewGuid(),Username="SuperAdmin",FullName="Admin",Password="test",IsActive=true };
var sheepA = new Livestock { Id=Guid.NewGuid(),Code="SA",Name="Sheep A",Sex="Female" };
var sheepB = new Livestock { Id=Guid.NewGuid(),Code="SB",Name="Sheep B",Sex="Female" };
var barnA = new Barn { Id=Guid.NewGuid(),Code="BRN-01",Name="Barn A",FkFarmId=a.Id,FkLivestockId=sheepA.Id,IsActive=true };
var barnB = new Barn { Id=Guid.NewGuid(),Code="BRN-010",Name="Barn B",FkFarmId=b.Id,FkLivestockId=sheepB.Id,IsActive=true };
var videoA = new SourceVideo { Id=Guid.NewGuid(),Code="VA",SourceVideoUrl="https://example.invalid/a",FkBarnId=barnA.Id,IsActive=true };
var videoB = new SourceVideo { Id=Guid.NewGuid(),Code="VB",SourceVideoUrl="https://example.invalid/b",FkBarnId=barnB.Id,IsActive=true };
var now = DateTime.Today.AddHours(10);
var statisticA = new SucklingStatistic { Id=Guid.NewGuid(),Code="identical-code",PeriodDescription="A",FkBarnId=barnA.Id,ActivityFrom=now,ActivityTo=now,RequiresReminder=true };
var statisticB = new SucklingStatistic { Id=Guid.NewGuid(),Code="identical-code",PeriodDescription="B",FkBarnId=barnB.Id,ActivityFrom=now,ActivityTo=now,RequiresReminder=true };
await using (var seed = new ApplicationDbContext(options,new HttpContextAccessor())) {
    foreach (var user in new[]{userA,userB,admin}) user.Email=user.Username+"@example.invalid";
    seed.AddRange(a,b,userA,userB,admin,sheepA,sheepB,barnA,barnB,videoA,videoB,statisticA,statisticB,
        new SucklingStatistic {Id=Guid.NewGuid(),Code="unassigned",PeriodDescription="Unknown"},
        new UserFarm {FkUserId=userA.Id,FkFarmId=a.Id,IsActive=true},
        new UserFarm {FkUserId=userB.Id,FkFarmId=b.Id,IsActive=true},
        new UserFarm {FkUserId=admin.Id,FkFarmId=a.Id,IsActive=true},
        new UserFarm {FkUserId=admin.Id,FkFarmId=b.Id,IsActive=true},
        new SucklingActivity {Id=Guid.NewGuid(),FkBarnId=barnA.Id,ActivityStart=now,TotalFrequency=5},
        new SucklingActivity {Id=Guid.NewGuid(),FkBarnId=barnB.Id,ActivityStart=now,TotalFrequency=99},
        new ReminderLog {Id=Guid.NewGuid(),FkSucklingStatisticId=statisticA.Id,Username="a",Status="Sent",ScheduledAt=now},
        new ReminderLog {Id=Guid.NewGuid(),FkSucklingStatisticId=statisticB.Id,Username="b",Status="Sent",ScheduledAt=now},
        new SourceVideoStatusLog {Id=Guid.NewGuid(),FkIdSourceVideo=videoA.Id,Code="a"},
        new SourceVideoStatusLog {Id=Guid.NewGuid(),FkIdSourceVideo=videoB.Id,Code="b"},
        new JobExecutionLog {Id=Guid.NewGuid(),Code="GLOBAL"},
        new SystemNotification {Id=Guid.NewGuid(),Username="a",FkFarmId=a.Id,Code="ReminderLog:A"},
        new SystemNotification {Id=Guid.NewGuid(),Username="a",FkFarmId=b.Id,Code="ReminderLog:B"},
        new SystemNotification {Id=Guid.NewGuid(),Username="a",Code="ReminderLog:unknown"},
        new SystemNotification {Id=Guid.NewGuid(),Username="b",FkFarmId=b.Id,Code="ReminderLog:B2"});
    await seed.SaveChangesAsync();
}
await using (var db = new ApplicationDbContext(options,Request("a"))) {
    Check(await db.Farm.CountAsync()==1 && await db.Barn.CountAsync()==1 && await db.Livestock.CountAsync()==1, "Master data and dropdowns only expose assigned farm");
    Check(await db.SourceVideo.CountAsync()==1 && await db.SourceVideoStatusLog.CountAsync()==1, "Sources and status logs are farm scoped");
    Check(await db.SucklingActivity.CountAsync()==1 && await db.SucklingStatistic.CountAsync()==1 && await db.ReminderLog.CountAsync()==1, "Activities, statistics and reminders are farm scoped");
    Check(await db.SystemNotification.CountAsync()==1 && await db.JobExecutionLog.CountAsync()==0, "Notification counts exclude other farms and global job summaries");
    Check(await db.Barn.FirstOrDefaultAsync(x=>x.Id==barnB.Id)==null && await db.SourceVideo.FindAsync(videoB.Id)==null, "Foreign IDs cannot bypass details filtering");
    var service = new SmartSheepReportService(new MasterDataRepositories(db),new SmartSheepRepositories(db));
    var dashboard = await service.GetDashboardAsync(new DashboardFilterParam {StartDate=now.Date,EndDate=now.Date});
    Check(dashboard.TotalFarms==1 && dashboard.TotalBarns==1 && dashboard.TodayFrequency==5 && dashboard.StatisticsRequiringReminder==1, "Dashboard aggregates and counts use assigned farms");
    Check(await service.GetSucklingStatistics(new SmartSheepReportFilterParam {FarmId=b.Id}).CountAsync()==0, "Explicit foreign farm filter cannot widen access");
    Check(await service.GetReminders(new SmartSheepReportFilterParam {BarnId=barnB.Id}).CountAsync()==0, "Foreign barn filter cannot widen reminder reports");
    var restrictedDashboard=await service.GetDashboardAsync(new DashboardFilterParam {StartDate=now.Date,EndDate=now.Date,FarmId=b.Id});
    Check(restrictedDashboard.TotalBarns==0 && restrictedDashboard.StatisticsRequiringReminder==0 && restrictedDashboard.Reminders.Sum(x=>x.Total)==0, "Selected unauthorized farm returns empty dashboard, including reminder totals");
}
await using (var db = new ApplicationDbContext(options,Request("b"))) Check((await db.Farm.SingleAsync()).Id==b.Id,"Scope is per request; a second user cannot inherit the first scope");
await using (var db = new ApplicationDbContext(options,Request("SuperAdmin"))) Check(await db.Farm.CountAsync()==2 && await db.JobExecutionLog.CountAsync()==1,"Super Admin sees all assigned farms and global summaries");
foreach (var user in new string?[]{"unassigned",null}) await using (var db = new ApplicationDbContext(options,Request(user)))
    Check(await db.Farm.CountAsync()==0 && await db.SourceVideo.CountAsync()==0 && await db.SucklingStatistic.CountAsync()==0,"Missing user or assignments fails closed");
await using (var db = new ApplicationDbContext(options,Request("a",false))) Check(await db.Farm.CountAsync()==0,"Unauthenticated claims do not grant farm access");
await using (var background = new ApplicationDbContext(options,new HttpContextAccessor { HttpContext = null })) {
    Check(await background.Farm.CountAsync()==2 && await background.SucklingStatistic.CountAsync()==3,"Hosted jobs retain background access");
    var assignment=await background.UserFarm.SingleAsync(x=>x.FkUserId==userA.Id); assignment.IsActive=false; await background.SaveChangesAsync();
}
await using (var db = new ApplicationDbContext(options,Request("a"))) Check(await db.Barn.CountAsync()==0 && await db.SystemNotification.CountAsync()==0,"Revocation applies on the next request without signing out");
// Generate actual SQL without opening a database connection.
await using(var sql = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options,Request("a"))) {
    foreach(var query in new[]{sql.Farm.ToQueryString(),sql.Barn.ToQueryString(),sql.Livestock.ToQueryString(),sql.SucklingStatistic.ToQueryString(),sql.ReminderLog.ToQueryString(),sql.SourceVideoStatusLog.ToQueryString(),sql.JobExecutionLog.ToQueryString(),sql.SystemNotification.ToQueryString()})
        Check(query.Contains("UserFarm"),"SQL Server query includes assignment enforcement");
}
