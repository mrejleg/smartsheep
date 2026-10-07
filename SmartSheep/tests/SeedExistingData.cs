#:project ../src/02.Applications/01.WebApi/02.Infrastructure/02.Infrastructure.csproj
#:package Microsoft.EntityFrameworkCore.InMemory@10.0.5
#:property PublishAot=false

using System.Collections;
using System.Globalization;
using Api.Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Core.Interfaces.Logging;

// Isolated in-memory database only; never reads application connection strings.
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
    .Options;
await using var db = new ApplicationDbContext(options, new HttpContextAccessor());
var logger = new TestLogger();
Task Seed() => ApplicationDbContextSeed.ApplicationDbContextSeedAsync(db, "test-only-key", "test-client", logger, 10);
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}

await Seed();
Check(await db.AppMenu.AnyAsync(x => x.Controller == "SheepBreeding" && x.IsMobile && x.AccessTypes == "view" && x.Icon != null)
    && await db.AppMenu.CountAsync(x => (x.Controller == "SucklingActivity" || x.Controller == "SucklingStatistic") && x.Icon != null && x.Icon == x.IconActive) == 2,
    "Fresh seed includes Sheep Breeding and meaningful suckling icons");
Check(await db.AppMenu.AnyAsync(x => x.Controller == "UserFarm" && !x.IsMobile && x.Icon == null && x.IconActive == null)
    && await db.ScopeClient.CountAsync(x => x.Name.StartsWith("UserFarm.")) == 4
    && await db.UserFarm.CountAsync() == await db.Farm.CountAsync(), "Fresh seed includes User Farms menu and assigns SuperAdmin to every farm");
Check(await db.AppMenu.AnyAsync() && await db.News.AnyAsync() && await db.Farm.AnyAsync(),
    "Empty database receives initial menus, news and sample graph");

var menu = await db.AppMenu.FirstAsync(x => x.Controller == "Dashboard");
menu.IsMobile = false;
menu.IsActive = false;
menu.Name = "Administrator dashboard name";
menu.Icon = "custom-icon";
menu.IconActive = "custom-active-icon";
menu.SequenceNumber = "custom-sequence";
var template = await db.ReminderTemplate.FirstAsync();
template.Name = "Administrator template";
template.ReminderText = "Do not overwrite this reminder";
template.IsActive = false;
var news = await db.News.FirstAsync();
news.Title = "Administrator news";
news.Content = "Administrator content";
news.IsActive = false;
var settings = await db.SystemConfig.FirstAsync();
settings.StatisticsDurationMinutes = 77;
settings.IsActive = false;
var revokedFarm = await db.UserFarm.FirstAsync();
revokedFarm.IsActive = false;
db.UserFarm.Remove(await db.UserFarm.FirstAsync(x => x.Id != revokedFarm.Id));
var home = await db.AppMenu.FirstAsync(x => x.Controller == "Home");
home.Icon = null;
home.IconActive = null;
db.AppMenu.Remove(await db.AppMenu.FirstAsync(x => x.Controller == "Farm"));
await db.SaveChangesAsync();

var before = Snapshot();
await Seed();
Check(Snapshot() == before, "Populated database is unchanged, including inactive rows, flags, icons, content and timestamps");
await Seed();
Check(Snapshot() == before, "Second startup does not insert, update or delete existing data");
Check(!await db.AppMenu.AnyAsync(x => x.Controller == "Farm"),
    "Missing menu is not restored when the menu table already contains data");

// Only the deliberately emptied table should be populated again.
db.News.RemoveRange(db.News);
await db.SaveChangesAsync();
var withoutNews = Snapshot("News");
await Seed();
Check(await db.News.CountAsync() == 50, "An empty News table is seeded independently");
Check(Snapshot("News") == withoutNews, "Seeding empty News leaves all other tables unchanged");

// A partially populated operational graph is not treated as an empty database.
db.UserFarm.RemoveRange(db.UserFarm);
await db.SaveChangesAsync();
db.Farm.RemoveRange(db.Farm);
await db.SaveChangesAsync();
var partial = Snapshot();
await Seed();
Check(!await db.Farm.AnyAsync() && Snapshot() == partial,
    "An existing operational graph does not receive new sample farms or activities");

string Snapshot(string? excluded = null)
{
    db.ChangeTracker.Clear();
    var rows = new List<string>();
    foreach (var property in typeof(ApplicationDbContext).GetProperties()
        .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
    {
        if (property.Name == excluded) continue;
        foreach (var entity in (IEnumerable)property.GetValue(db)!)
        {
            var entry = db.Entry(entity);
            rows.Add(property.Name + ":" + string.Join("|", entry.Properties.OrderBy(p => p.Metadata.Name)
                .Select(p => p.Metadata.Name + "=" + (p.CurrentValue is DateTime date
                    ? date.ToString("O", CultureInfo.InvariantCulture)
                    : Convert.ToString(p.CurrentValue, CultureInfo.InvariantCulture)))));
        }
    }
    return string.Join("\n", rows.OrderBy(x => x, StringComparer.Ordinal));
}

sealed class TestLogger : IAppLogger<ApplicationDbContext>
{
    public void LogInformation(string message, params object[] args) { }
    public void LogWarning(string message, params object[] args) { }
    public void LogError(string message, params object[] args) => throw new Exception(message);
}
