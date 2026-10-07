using Api.Domain.ApiManagements;
using Api.Domain.Entities.Configs;
using Microsoft.EntityFrameworkCore;
using Project.Core.Extentions;
using Project.Core.Interfaces.Logging;

namespace Api.Infrastructure.Data.DbContexts.SeedingData
{
    public class ConfigDbContextSeed
    {
        private const string SmartSheepClientId = "SmartSheep";

        private static readonly string[] ApiScopeNames =
        {
            "AppClient.Add", "AppClient.Delete", "AppClient.Edit", "AppClient.View",
            "AppMenu.Add", "AppMenu.Delete", "AppMenu.Edit", "AppMenu.View",
            "AppMenuRole.Add", "AppMenuRole.Delete", "AppMenuRole.Edit", "AppMenuRole.View",
            "AppRole.Add", "AppRole.Delete", "AppRole.Edit", "AppRole.View",
            "AppScopeClient.Add", "AppScopeClient.Delete", "AppScopeClient.Edit", "AppScopeClient.View",
            "ScopeClient.Add", "ScopeClient.Delete", "ScopeClient.Edit", "ScopeClient.View",
            "EmailTemplate.Add", "EmailTemplate.Delete", "EmailTemplate.Edit", "EmailTemplate.View",
            "Farm.Add", "Farm.Delete", "Farm.Edit", "Farm.View",
            "UserFarm.Add", "UserFarm.Delete", "UserFarm.Edit", "UserFarm.View",
            "Barn.Add", "Barn.Delete", "Barn.Edit", "Barn.View",
            "Livestock.Add", "Livestock.Delete", "Livestock.Edit", "Livestock.View",
            "SourceVideo.Add", "SourceVideo.Delete", "SourceVideo.Edit", "SourceVideo.View",
            "SystemConfig.Add", "SystemConfig.Delete", "SystemConfig.Edit", "SystemConfig.View",
            "ReminderTemplate.Add", "ReminderTemplate.Delete", "ReminderTemplate.Edit", "ReminderTemplate.View",
            "SucklingActivity.Add", "SucklingActivity.Delete", "SucklingActivity.Edit", "SucklingActivity.View",
            "SucklingStatistic.Add", "SucklingStatistic.Delete", "SucklingStatistic.Edit", "SucklingStatistic.View",
            "SourceVideoStatusLog.Add", "SourceVideoStatusLog.Delete", "SourceVideoStatusLog.Edit", "SourceVideoStatusLog.View",
            "JobExecutionLog.Add", "JobExecutionLog.Delete", "JobExecutionLog.Edit", "JobExecutionLog.View",
            "ReminderLog.Add", "ReminderLog.Delete", "ReminderLog.Edit", "ReminderLog.View",
            "News.Add", "News.Delete", "News.Edit", "News.View",
            "FirebaseNotification.Add",
            "SmartSheepReport.View"
        };

        public static async Task ConfigSeedAsync(ApplicationDbContext db, string encryptKey, string clientId, IAppLogger<ApplicationDbContext> appLogger, int? retry = 0)
        {
            var retryCount = retry ?? 0;
            var previousTrackingBehavior = db.ChangeTracker.QueryTrackingBehavior;
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            try
            {
                // Seed only empty tables. Existing configuration (including
                // inactive rows) belongs to the administrator, not startup.
                if (!await db.AppClient.AnyAsync()) await AddAppClientAsync(db, encryptKey);
                if (!await db.ScopeClient.AnyAsync()) await AddScopeClientsAsync(db);
                if (!await db.AppScopeClient.AnyAsync()) await AddAppScopeClientsAsync(db);
                if (!await db.AppRole.AnyAsync()) await AddAppRoleAsync(db);
                if (!await db.AppMenu.AnyAsync()) await AddBaseMenusAsync(db);
                if (!await db.AppMenuRole.AnyAsync()) await AddAppMenuRolesAsync(db);
            }
            catch (Exception ex)
            {
                if (retryCount < 10)
                {
                    appLogger.LogError(ex.Message);
                    db.ChangeTracker.Clear();
                    await ConfigSeedAsync(db, encryptKey, clientId, appLogger, retryCount + 1);
                    return;
                }

                throw;
            }
            finally
            {
                db.ChangeTracker.QueryTrackingBehavior = previousTrackingBehavior;
            }
        }

        public static Task ReconcileAppMenuRolesAsync(ApplicationDbContext db)
        {
            return AddAppMenuRolesAsync(db);
        }

        private static async Task AddAppClientAsync(ApplicationDbContext db, string encryptKey)
        {
            var matchingClients = db.AppClient
                .ToList()
                .Where(x => string.Equals(NormalizeKey(x.ClientId), SmartSheepClientId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .ToList();
            var client = matchingClients.FirstOrDefault();
            if (client == null)
            {
                db.AppClient.Add(new AppClient
                {
                    ClientId = SmartSheepClientId,
                    ClientSecret = TextExtentions.GetEncodeTextMd5(SmartSheepClientId, encryptKey),
                    Name = "SmartSheep",
                    Description = "SmartSheep Livestock Management Platform",
                    ExpiredInSecond = 86400,
                    CreatedBy = "system",
                    ModifiedBy = "system",
                    IsActive = true
                });
                await db.SaveChangesAsync();
                return;
            }

            var canonicalMappings = db.AppScopeClient.Where(x => x.FkAppClientId == client.Id).ToList();
            foreach (var duplicateClient in matchingClients.Skip(1))
            {
                var duplicateMappings = db.AppScopeClient.Where(x => x.FkAppClientId == duplicateClient.Id).ToList();
                foreach (var duplicateMapping in duplicateMappings)
                {
                    var existingMapping = canonicalMappings.FirstOrDefault(x =>
                        x.FkScopeClientId == duplicateMapping.FkScopeClientId);
                    if (existingMapping == null)
                    {
                        duplicateMapping.FkAppClientId = client.Id;
                        duplicateMapping.ModifiedBy = "system";
                        canonicalMappings.Add(duplicateMapping);
                        continue;
                    }

                    existingMapping.IsActive = existingMapping.IsActive || duplicateMapping.IsActive;
                    existingMapping.ModifiedBy = "system";
                    db.AppScopeClient.Remove(duplicateMapping);
                }

                db.AppClient.Remove(duplicateClient);
            }

            client.ClientId = SmartSheepClientId;
            client.Name = "SmartSheep";
            client.ClientSecret = TextExtentions.GetEncodeTextMd5(SmartSheepClientId, encryptKey);
            client.Description = "SmartSheep Livestock Management Platform";
            client.ExpiredInSecond = 86400;
            client.ModifiedBy = "system";
            client.IsActive = true;
            await db.SaveChangesAsync();
        }

        private static async Task AddScopeClientsAsync(ApplicationDbContext db)
        {
            var existingScopes = db.ScopeClient.ToList();
            var mappings = db.AppScopeClient.ToList();
            foreach (var name in ApiScopeNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var matchingScopes = existingScopes
                    .Where(x => string.Equals(NormalizeKey(x.Name), name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.DateCreated)
                    .ThenBy(x => x.Id)
                    .ToList();
                var scope = matchingScopes.FirstOrDefault();
                if (scope == null)
                {
                    scope = new ScopeClient
                    {
                        Name = name,
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        IsActive = true
                    };
                    db.ScopeClient.Add(scope);
                    existingScopes.Add(scope);
                }

                foreach (var duplicateScope in matchingScopes.Skip(1))
                {
                    var duplicateMappings = mappings.Where(x => x.FkScopeClientId == duplicateScope.Id).ToList();
                    foreach (var duplicateMapping in duplicateMappings)
                    {
                        var existingMapping = mappings.FirstOrDefault(x =>
                            x.FkAppClientId == duplicateMapping.FkAppClientId &&
                            x.FkScopeClientId == scope.Id &&
                            !ReferenceEquals(x, duplicateMapping));
                        if (existingMapping == null)
                        {
                            duplicateMapping.FkScopeClientId = scope.Id;
                            duplicateMapping.ModifiedBy = "system";
                            continue;
                        }

                        existingMapping.IsActive = existingMapping.IsActive || duplicateMapping.IsActive;
                        existingMapping.ModifiedBy = "system";
                        db.AppScopeClient.Remove(duplicateMapping);
                        mappings.Remove(duplicateMapping);
                    }

                    db.ScopeClient.Remove(duplicateScope);
                    existingScopes.Remove(duplicateScope);
                }

                scope.Name = name;
                scope.IsActive = true;
                scope.ModifiedBy = "system";
            }
            await db.SaveChangesAsync();
        }

        private static async Task AddAppScopeClientsAsync(ApplicationDbContext db)
        {
            var client = db.AppClient.First(x => x.ClientId == SmartSheepClientId);
            var scopeNames = new HashSet<string>(ApiScopeNames, StringComparer.OrdinalIgnoreCase);
            var scopes = db.ScopeClient.ToList().Where(x => scopeNames.Contains(x.Name)).ToList();
            var mappingGroups = db.AppScopeClient
                .Where(x => x.FkAppClientId == client.Id && x.FkScopeClientId.HasValue)
                .ToList()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .GroupBy(x => x.FkScopeClientId!.Value)
                .ToList();
            var existing = mappingGroups.ToDictionary(group => group.Key, group => group.First());
            foreach (var duplicate in mappingGroups.SelectMany(group => group.Skip(1)))
            {
                var canonical = existing[duplicate.FkScopeClientId!.Value];
                canonical.IsActive = canonical.IsActive || duplicate.IsActive;
                canonical.ModifiedBy = "system";
                db.AppScopeClient.Remove(duplicate);
            }

            foreach (var mapping in existing.Values.Where(x => x.FkScopeClientId.HasValue && !scopes.Any(scope => scope.Id == x.FkScopeClientId.Value)))
            {
                mapping.IsActive = false;
                mapping.ModifiedBy = "system";
            }

            foreach (var scope in scopes)
            {
                if (existing.TryGetValue(scope.Id, out var mapping))
                {
                    mapping.IsActive = true;
                    mapping.ModifiedBy = "system";
                    continue;
                }

                db.AppScopeClient.Add(new AppScopeClient
                {
                    FkAppClientId = client.Id,
                    FkScopeClientId = scope.Id,
                    CreatedBy = "system",
                    ModifiedBy = "system",
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();
        }

        private static async Task AddAppRoleAsync(ApplicationDbContext db)
        {
            var matchingRoles = db.AppRole
                .ToList()
                .Where(x => string.Equals(NormalizeKey(x.Name), "SUPER.ADMIN", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .ToList();
            var role = matchingRoles.FirstOrDefault();
            if (role == null)
            {
                db.AppRole.Add(new AppRole { Name = "SUPER.ADMIN", CreatedBy = "system", ModifiedBy = "system", IsActive = true });
                await db.SaveChangesAsync();
                return;
            }

            var menuRoles = db.AppMenuRole.ToList();
            var userRoles = db.UserLoginRole.ToList();
            foreach (var duplicateRole in matchingRoles.Skip(1))
            {
                foreach (var duplicateMenuRole in menuRoles.Where(x => x.FkAppRoleId == duplicateRole.Id).ToList())
                {
                    var canonicalMenuRole = menuRoles.FirstOrDefault(x =>
                        x.FkAppRoleId == role.Id &&
                        x.FkAppMenuId == duplicateMenuRole.FkAppMenuId);
                    if (canonicalMenuRole == null)
                    {
                        duplicateMenuRole.FkAppRoleId = role.Id;
                        duplicateMenuRole.ModifiedBy = "system";
                        continue;
                    }

                    canonicalMenuRole.AccessTypes = MergeAccessTypes(canonicalMenuRole.AccessTypes, duplicateMenuRole.AccessTypes);
                    canonicalMenuRole.IsActive = canonicalMenuRole.IsActive || duplicateMenuRole.IsActive;
                    canonicalMenuRole.ModifiedBy = "system";
                    db.AppMenuRole.Remove(duplicateMenuRole);
                    menuRoles.Remove(duplicateMenuRole);
                }

                foreach (var duplicateUserRole in userRoles.Where(x => x.FkRoleId == duplicateRole.Id).ToList())
                {
                    var canonicalUserRole = userRoles.FirstOrDefault(x =>
                        x.FkRoleId == role.Id &&
                        x.FkUserId == duplicateUserRole.FkUserId);
                    if (canonicalUserRole == null)
                    {
                        duplicateUserRole.FkRoleId = role.Id;
                        duplicateUserRole.ModifiedBy = "system";
                        continue;
                    }

                    canonicalUserRole.IsActive = canonicalUserRole.IsActive || duplicateUserRole.IsActive;
                    canonicalUserRole.ModifiedBy = "system";
                    db.UserLoginRole.Remove(duplicateUserRole);
                    userRoles.Remove(duplicateUserRole);
                }

                db.AppRole.Remove(duplicateRole);
            }

            role.Name = "SUPER.ADMIN";
            role.ModifiedBy = "system";
            role.IsActive = true;
            await db.SaveChangesAsync();
        }

        private static async Task AddBaseMenusAsync(ApplicationDbContext db)
        {
            var definitions = new[]
            {
                new { Name = "Home", Controller = "Home", Sequence = "a.0", Access = "view", Section = false },
                new { Name = "FEATURES", Controller = (string)null, Sequence = "b.0", Access = "view", Section = true },
                new { Name = "Dashboard", Controller = "Dashboard", Sequence = "b.1", Access = "view", Section = false },
                new { Name = "SETTINGS", Controller = (string)null, Sequence = "c.0", Access = "view", Section = true },
                new { Name = "Master Data", Controller = (string)null, Sequence = "c.1", Access = "view", Section = false },
                new { Name = "Configs", Controller = (string)null, Sequence = "c.2", Access = "view", Section = false },
                new { Name = "API", Controller = (string)null, Sequence = "c.3", Access = "view", Section = false }
            };

            foreach (var definition in definitions)
            {
                var menu = db.AppMenu.FirstOrDefault(x => x.Name == definition.Name && x.Controller == definition.Controller);
                if (menu == null)
                {
                    db.AppMenu.Add(new AppMenu
                    {
                        Name = definition.Name,
                        Controller = definition.Controller,
                        SequenceNumber = definition.Sequence,
                        AccessTypes = definition.Access,
                        IsSection = definition.Section,
                        IsMobile = true,
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        IsActive = true
                    });
                    continue;
                }

                menu.SequenceNumber = definition.Sequence;
                menu.AccessTypes = definition.Access;
                menu.IsSection = definition.Section;
                menu.ModifiedBy = "system";
                menu.IsActive = true;
            }
            await db.SaveChangesAsync();
        }

        private static async Task AddAppMenuRolesAsync(ApplicationDbContext db)
        {
            var role = db.AppRole.First(x => x.Name == "SUPER.ADMIN");
            var menuRoleGroups = db.AppMenuRole
                .Where(x => x.FkAppRoleId == role.Id && x.FkAppMenuId.HasValue)
                .ToList()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .GroupBy(x => x.FkAppMenuId!.Value)
                .ToList();
            var existing = menuRoleGroups.ToDictionary(group => group.Key, group => group.First());
            var duplicateMenuRoles = menuRoleGroups.SelectMany(group => group.Skip(1));
            foreach (var duplicate in duplicateMenuRoles)
            {
                var canonical = existing[duplicate.FkAppMenuId!.Value];
                canonical.AccessTypes = MergeAccessTypes(canonical.AccessTypes, duplicate.AccessTypes);
                canonical.IsActive = canonical.IsActive || duplicate.IsActive;
                canonical.ModifiedBy = "system";
                db.AppMenuRole.Remove(duplicate);
            }

            foreach (var menu in db.AppMenu.ToList())
            {
                if (existing.TryGetValue(menu.Id, out var menuRole))
                {
                    menuRole.AccessTypes = menu.AccessTypes;
                    menuRole.ModifiedBy = "system";
                    menuRole.IsActive = menu.IsActive;
                    continue;
                }

                db.AppMenuRole.Add(new AppMenuRole
                {
                    FkAppMenuId = menu.Id,
                    FkAppRoleId = role.Id,
                    AccessTypes = menu.AccessTypes,
                    CreatedBy = "system",
                    ModifiedBy = "system",
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();
        }

        private static string MergeAccessTypes(params string?[] values)
        {
            return string.Join(",", values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string? value) => (value ?? string.Empty).Trim();
    }
}
