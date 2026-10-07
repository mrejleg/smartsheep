using Api.Domain.ApiManagements;
using Api.Domain.Entities.Configs;
using Api.Domain.Entities.Notifications;
using Api.Domain.Entities.Auths;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Entities.MasterDatas;
using Api.Domain.Entities.MobileApplications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Project.Core.AppContext;
using Project.Core.Extentions;

namespace Api.Infrastructure.Data.DbContexts
{
    public class ApplicationDbContext : BaseDbContextAction
    {
        private readonly IHttpContextAccessor _request;
        public bool FarmScopeRequired => _request.HttpContext != null;
        public string CurrentUsername
        {
            get
            {
                if (_request.HttpContext?.User.Identity?.IsAuthenticated != true) return string.Empty;
                try { return _request.HttpContext.User.FindFirst("Username")?.Value.ToBase64Decode() ?? string.Empty; }
                catch (FormatException) { return string.Empty; }
            }
        }
        public IQueryable<Guid> AssignedFarmIds => UserFarm.Where(x => x.IsActive && x.UserLogin!.IsActive &&
            x.UserLogin.Username == CurrentUsername).Select(x => x.FkFarmId);

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options, httpContextAccessor)
        {
            _request = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserDeviceToken>()
                .HasIndex(x => x.DeviceToken)
                .IsUnique();
            modelBuilder.Entity<UserFarm>().HasIndex(x => new { x.FkUserId, x.FkFarmId }).IsUnique();
            modelBuilder.Entity<UserFarm>().HasOne(x => x.UserLogin).WithMany()
                .HasForeignKey(x => x.FkUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserFarm>().HasOne(x => x.Farm).WithMany()
                .HasForeignKey(x => x.FkFarmId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SucklingStatistic>().HasOne(x => x.Barn).WithMany()
                .HasForeignKey(x => x.FkBarnId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SystemNotification>().HasOne<Farm>().WithMany()
                .HasForeignKey(x => x.FkFarmId).OnDelete(DeleteBehavior.Restrict);

            // Scope every HTTP request, including missing/invalid user claims.
            // Hosted jobs and bootstrap run without an HTTP context. No role bypass.
            modelBuilder.Entity<Farm>().HasQueryFilter(x => !FarmScopeRequired || UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername && a.FkFarmId == x.Id));
            modelBuilder.Entity<Barn>().HasQueryFilter(x => !FarmScopeRequired ||
                UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername && a.FkFarmId == x.FkFarmId));
            modelBuilder.Entity<Livestock>().HasQueryFilter(x => !FarmScopeRequired || Barn.Any(b => b.FkLivestockId == x.Id));
            modelBuilder.Entity<SourceVideo>().HasQueryFilter(x => !FarmScopeRequired || Barn.Any(b => b.Id == x.FkBarnId));
            modelBuilder.Entity<SucklingActivity>().HasQueryFilter(x => !FarmScopeRequired || Barn.Any(b => b.Id == x.FkBarnId));
            modelBuilder.Entity<SucklingStatistic>().HasQueryFilter(x => !FarmScopeRequired || Barn.Any(b => b.Id == x.FkBarnId));
            modelBuilder.Entity<SucklingEvent>().HasQueryFilter(x => !FarmScopeRequired || Barn.Any(b => b.Id == x.FkBarnId));
            modelBuilder.Entity<SourceVideoStatusLog>().HasQueryFilter(x => !FarmScopeRequired || SourceVideo.Any(v => v.Id == x.FkIdSourceVideo));
            modelBuilder.Entity<ReminderLog>().HasQueryFilter(x => !FarmScopeRequired || SucklingStatistic.Any(s => s.Id == x.FkSucklingStatisticId));
            // Global job summaries can contain data from multiple farms.
            modelBuilder.Entity<JobExecutionLog>().HasQueryFilter(x => !FarmScopeRequired ||
                (UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername) &&
                 !Farm.IgnoreQueryFilters().Any(f => f.IsActive && !UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername && a.FkFarmId == f.Id))));
            modelBuilder.Entity<SystemNotification>().HasQueryFilter(x => !FarmScopeRequired ||
                (x.Username == CurrentUsername && (x.FkFarmId.HasValue
                    ? UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername && a.FkFarmId == x.FkFarmId.Value)
                    : (x.Code == null || (!x.Code.StartsWith("ReminderLog:") && !x.Code.StartsWith("SourceVideoStatusLog:") &&
                        (!x.Code.StartsWith("JobExecutionLog:") ||
                         (UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername) &&
                          !Farm.IgnoreQueryFilters().Any(f => f.IsActive && !UserFarm.Any(a => a.IsActive && a.UserLogin!.IsActive && a.UserLogin.Username == CurrentUsername && a.FkFarmId == f.Id)))))))));
        }

        #region Configs

        public DbSet<AppMenu> AppMenu { get; set; }
        public DbSet<AppMenuRole> AppMenuRole { get; set; }
        public DbSet<AppRole> AppRole { get; set; }
        public DbSet<AppClient> AppClient { get; set; }
        public DbSet<ScopeClient> ScopeClient { get; set; }
        public DbSet<AppScopeClient> AppScopeClient { get; set; }
        public DbSet<EmailTemplate> EmailTemplate { get; set; }
        public DbSet<SystemConfig> SystemConfig { get; set; }
        public DbSet<ReminderTemplate> ReminderTemplate { get; set; }
        public DbSet<News> News { get; set; }

        #endregion

        #region Auths

        public DbSet<UserLogin> UserLogin { get; set; }
        public DbSet<UserLoginRole> UserLoginRole { get; set; }
        public DbSet<UserFarm> UserFarm { get; set; }

        #endregion

        #region Notifications

        public DbSet<SystemNotification> SystemNotification { get; set; }
        public DbSet<UserDeviceToken> UserDeviceToken { get; set; }

        #endregion

        #region MobileApplications

        public DbSet<MenuFavorite> MenuFavorite { get; set; }

        #endregion

        #region MasterDatas

        public DbSet<Farm> Farm { get; set; }
        public DbSet<Barn> Barn { get; set; }
        public DbSet<Livestock> Livestock { get; set; }
        public DbSet<SourceVideo> SourceVideo { get; set; }

        #endregion

        #region SmartSheep

        public DbSet<SucklingActivity> SucklingActivity { get; set; }
        public DbSet<SucklingStatistic> SucklingStatistic { get; set; }
        public DbSet<SucklingEvent> SucklingEvent { get; set; }
        public DbSet<ReminderLog> ReminderLog { get; set; }
        public DbSet<SourceVideoStatusLog> SourceVideoStatusLog { get; set; }
        public DbSet<JobExecutionLog> JobExecutionLog { get; set; }

        #endregion

    }
}
