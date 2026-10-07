using EntityFrameworkCore.Triggers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Project.Base.Models.Entities;
using Project.Core.Extentions;
using System.Text.RegularExpressions;

namespace Project.Core.AppContext
{
    public class BaseDbContextAction : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseDbContextAction(DbContextOptions options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;
                    var isPrimaryKey = property.IsPrimaryKey();
                    var isForeignKey = property.IsForeignKey();

                    if (isPrimaryKey && clrType == typeof(Guid))
                    {
                        property.SetDefaultValueSql("NEWID()");
                    }

                    if (isForeignKey && clrType == typeof(Guid))
                    {
                        property.SetMaxLength(20);
                    }
                }

            }
        }

        public override int SaveChanges()
        {
            AddTimestamps();
            return this.SaveChangesWithTriggers(base.SaveChanges, acceptAllChangesOnSuccess: true);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddTimestamps();
            return this.SaveChangesWithTriggers(base.SaveChanges, acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return this.SaveChangesWithTriggersAsync(base.SaveChangesAsync, acceptAllChangesOnSuccess: true, cancellationToken: cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return this.SaveChangesWithTriggersAsync(base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
        }

        public void InjectViewSqlServer(DatabaseFacade db, string sqlQuery, string viewName)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                throw new ArgumentException("View SQL cannot be empty.", nameof(sqlQuery));
            }

            var viewParts = viewName?.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (viewParts is null || viewParts.Length is < 1 or > 2 ||
                viewParts.Any(part => !Regex.IsMatch(part, "^[A-Za-z_][A-Za-z0-9_]*$")))
            {
                throw new ArgumentException("View name must be a valid one- or two-part SQL identifier.", nameof(viewName));
            }

            var qualifiedViewName = string.Join('.', viewParts.Select(part => $"[{part}]"));
            db.ExecuteSqlRaw("IF OBJECT_ID({0}, 'V') IS NOT NULL EXEC('DROP VIEW " + qualifiedViewName + "')", viewName!);
            db.ExecuteSqlRaw($"CREATE VIEW {qualifiedViewName} AS {sqlQuery}");
        }

        private void AddTimestamps()
        {
            var entities = ChangeTracker.Entries().Where(x => x.Entity is IAuditableEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((IAuditableEntity)entity.Entity).DateCreated = DateTime.Now;
                    ((IAuditableEntity)entity.Entity).DateModified = DateTime.Now;

                    if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value.ToBase64Decode()))
                    {
                        ((IAuditableEntity)entity.Entity).CreatedBy = _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value.ToBase64Decode();
                        ((IAuditableEntity)entity.Entity).ModifiedBy = _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value.ToBase64Decode();
                    }
                }
                else
                {
                    foreach (var prop in entity.Properties)
                    {
                        if (prop.Metadata.Name == "DateCreated" || prop.Metadata.Name == "CreatedBy")
                        {
                            prop.IsModified = false;
                        }
                    }

                    ((IAuditableEntity)entity.Entity).DateModified = DateTime.Now;
                    if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value.ToBase64Decode()))
                    {
                        ((IAuditableEntity)entity.Entity).ModifiedBy = _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value.ToBase64Decode();
                    }
                }
            }
        }
    }
}
