using Api.Infrastructure.Data.DbContexts.SeedingData;
using Project.Core.Interfaces.Logging;
using Api.Domain.Entities.Auths;
using Api.Domain.Entities.Configs;
using Api.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Entities.MasterDatas;

namespace Api.Infrastructure.Data.DbContexts
{
    public class ApplicationDbContextSeed
    {
        public static async Task ApplicationDbContextSeedAsync(ApplicationDbContext applicationDbContext, string encryptKey, string clientId, IAppLogger<ApplicationDbContext> appLogger, int? retry = 0)
        {
            int retryForAvailability = retry ?? 0;
            try
            {
                var strategy = applicationDbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    applicationDbContext.ChangeTracker.Clear();
                    await using var transaction = await applicationDbContext.Database.BeginTransactionAsync();
                    var seedMenus = !await applicationDbContext.AppMenu.AnyAsync();
                    await ConfigDbContextSeed.ConfigSeedAsync(applicationDbContext, encryptKey, clientId, appLogger);
                    if (!await applicationDbContext.UserLogin.AnyAsync())
                    {
                        await AddSuperAdminAsync(applicationDbContext);
                    }
                    if (seedMenus)
                    {
                        await AddLocalUserMenusAsync(applicationDbContext);
                        await AddSmartSheepDataAndMenusAsync(applicationDbContext);
                        await AddMenuIconsAsync(applicationDbContext);
                    }
                    await AddSmartSheepDataAsync(applicationDbContext);
                    await SeedSuperAdminFarmsAsync(applicationDbContext);
                    await AddDetikNewsSeedAsync(applicationDbContext);
                    await transaction.CommitAsync();
                });
            }
            catch (Exception ex)
            {
                if (retryForAvailability < 10)
                {
                    retryForAvailability++;
                    appLogger.LogError(ex.Message.ToString());

                    applicationDbContext.ChangeTracker.Clear();
                    await ApplicationDbContextSeedAsync(applicationDbContext, encryptKey, clientId, appLogger, retryForAvailability);
                    return;
                }
                throw;
            }
        }

        public static async Task FixMenuIconsAsync(ApplicationDbContext applicationDbContext)
        {
            await AddMenuIconsAsync(applicationDbContext);
        }

        public static async Task SeedSuperAdminFarmsAsync(ApplicationDbContext db)
        {
            // Bootstrap only. Populated assignment tables are never overwritten.
            if (await db.UserFarm.AnyAsync()) return;
            var users = await db.UserLogin.Where(x => x.IsActive && x.Username == "SuperAdmin").Select(x => x.Id).ToListAsync();
            var farms = await db.Farm.IgnoreQueryFilters().Select(x => x.Id).ToListAsync();
            foreach (var userId in users)
                foreach (var farmId in farms)
                    db.UserFarm.Add(new UserFarm { Id = Guid.NewGuid(), FkUserId = userId, FkFarmId = farmId,
                        IsActive = true, CreatedBy = "super-admin-farm-seed", ModifiedBy = "super-admin-farm-seed" });
            await db.SaveChangesAsync();
        }

        private static async Task RenameMenusAsync(ApplicationDbContext db)
        {
            await MergeLegacyRootMenusAsync(db, "Livestocks", "Farm Transactions", "Farms");
            await MergeLegacyRootMenusAsync(db, "Reports", "Livestock Reports");
            await MergeLegacyRootMenusAsync(db, "API", "API Management", "Api Managements");
            await DeduplicateMenusAsync(db);
        }

        private static async Task MergeLegacyRootMenusAsync(ApplicationDbContext db, string canonicalName, params string[] legacyNames)
        {
            var candidateNames = new HashSet<string>(
                legacyNames.Append(canonicalName).Select(NormalizeKey),
                StringComparer.OrdinalIgnoreCase);
            var candidates = (await db.AppMenu.AsTracking()
                .Where(x => x.Controller == null &&
                            (x.FkParentId == null || x.FkParentId == Guid.Empty))
                .ToListAsync())
                .Where(x => candidateNames.Contains(NormalizeKey(x.Name)))
                .OrderBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var canonical = candidates.FirstOrDefault(x =>
                string.Equals(x.Name?.Trim(), canonicalName, StringComparison.OrdinalIgnoreCase)) ?? candidates[0];
            canonical.Name = canonicalName;
            canonical.ModifiedBy = "system";

            foreach (var duplicate in candidates.Where(x => x.Id != canonical.Id))
            {
                await MergeMenuIntoAsync(db, canonical, duplicate);
                await db.SaveChangesAsync();
            }

            await db.SaveChangesAsync();
        }

        private static async Task DeduplicateMenusAsync(ApplicationDbContext db)
        {
            var rootGroups = (await db.AppMenu.AsTracking()
                    .Where(x => x.Controller == null && (x.FkParentId == null || x.FkParentId == Guid.Empty))
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.DateCreated)
                    .ThenBy(x => x.Id)
                    .ToListAsync())
                .GroupBy(x => NormalizeKey(x.Name), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Key.Length > 0 && group.Count() > 1)
                .ToList();

            await MergeMenuGroupsAsync(db, rootGroups);

            var controllerGroups = (await db.AppMenu.AsTracking()
                    .Where(x => x.Controller != null && x.Controller != string.Empty)
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.DateCreated)
                    .ThenBy(x => x.Id)
                    .ToListAsync())
                .GroupBy(x => NormalizeKey(x.Controller), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Key.Length > 0 && group.Count() > 1)
                .ToList();

            await MergeMenuGroupsAsync(db, controllerGroups);

            var childGroupMenus = await db.AppMenu.AsTracking()
                .Where(x => x.Controller == null && x.FkParentId != null && x.FkParentId != Guid.Empty)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DateCreated)
                .ThenBy(x => x.Id)
                .ToListAsync();
            var childGroups = childGroupMenus
                .GroupBy(x => $"{x.FkParentId:N}|{NormalizeKey(x.Name)}", StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .ToList();

            await MergeMenuGroupsAsync(db, childGroups);
            await DeduplicateMenuRelationsAsync(db);
        }

        private static async Task DeduplicateMenuRelationsAsync(ApplicationDbContext db)
        {
            var roleGroups = (await db.AppMenuRole.AsTracking()
                    .Where(x => x.FkAppMenuId.HasValue && x.FkAppRoleId.HasValue)
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.DateCreated)
                    .ThenBy(x => x.Id)
                    .ToListAsync())
                .GroupBy(x => (x.FkAppMenuId!.Value, x.FkAppRoleId!.Value))
                .Where(group => group.Count() > 1)
                .ToList();
            foreach (var group in roleGroups)
            {
                var canonical = group.First();
                foreach (var duplicate in group.Skip(1))
                {
                    canonical.AccessTypes = MergeAccessTypes(canonical.AccessTypes, duplicate.AccessTypes);
                    canonical.IsActive = canonical.IsActive || duplicate.IsActive;
                    canonical.ModifiedBy = "system";
                    db.AppMenuRole.Remove(duplicate);
                }
            }

            var favoriteGroups = (await db.MenuFavorite.AsTracking()
                    .Where(x => x.FkAppMenuId.HasValue)
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.DateCreated)
                    .ThenBy(x => x.Id)
                    .ToListAsync())
                .GroupBy(
                    x => $"{x.FkAppMenuId:N}|{NormalizeKey(x.Email)}",
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .ToList();
            foreach (var group in favoriteGroups)
            {
                var canonical = group.First();
                foreach (var duplicate in group.Skip(1))
                {
                    canonical.IsActive = canonical.IsActive || duplicate.IsActive;
                    canonical.ModifiedBy = "system";
                    db.MenuFavorite.Remove(duplicate);
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task MergeMenuGroupsAsync(
            ApplicationDbContext db,
            IEnumerable<IGrouping<string, AppMenu>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                var canonical = group.First();
                foreach (var duplicate in group.Skip(1))
                {
                    await MergeMenuIntoAsync(db, canonical, duplicate);
                    await db.SaveChangesAsync();
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task MergeMenuIntoAsync(ApplicationDbContext db, AppMenu canonical, AppMenu duplicate)
        {
            if (canonical.Id == duplicate.Id)
            {
                return;
            }

            var children = await db.AppMenu.AsTracking().Where(x => x.FkParentId == duplicate.Id).ToListAsync();
            foreach (var child in children)
            {
                child.FkParentId = canonical.Id;
                child.ModifiedBy = "system";
            }

            var canonicalRoles = await db.AppMenuRole.AsTracking().Where(x => x.FkAppMenuId == canonical.Id).ToListAsync();
            var duplicateRoles = await db.AppMenuRole.AsTracking().Where(x => x.FkAppMenuId == duplicate.Id).ToListAsync();
            foreach (var duplicateRole in duplicateRoles)
            {
                var canonicalRole = canonicalRoles.FirstOrDefault(x =>
                    x.FkAppRoleId == duplicateRole.FkAppRoleId &&
                    db.Entry(x).State != EntityState.Deleted);
                if (canonicalRole == null)
                {
                    duplicateRole.FkAppMenuId = canonical.Id;
                    duplicateRole.ModifiedBy = "system";
                    canonicalRoles.Add(duplicateRole);
                    continue;
                }

                canonicalRole.AccessTypes = MergeAccessTypes(canonicalRole.AccessTypes, duplicateRole.AccessTypes);
                canonicalRole.IsActive = canonicalRole.IsActive || duplicateRole.IsActive;
                canonicalRole.ModifiedBy = "system";
                db.AppMenuRole.Remove(duplicateRole);
            }

            var canonicalFavorites = await db.MenuFavorite.AsTracking().Where(x => x.FkAppMenuId == canonical.Id).ToListAsync();
            var duplicateFavorites = await db.MenuFavorite.AsTracking().Where(x => x.FkAppMenuId == duplicate.Id).ToListAsync();
            foreach (var duplicateFavorite in duplicateFavorites)
            {
                var canonicalFavorite = canonicalFavorites.FirstOrDefault(x =>
                    string.Equals(x.Email?.Trim(), duplicateFavorite.Email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    db.Entry(x).State != EntityState.Deleted);
                if (canonicalFavorite == null)
                {
                    duplicateFavorite.FkAppMenuId = canonical.Id;
                    duplicateFavorite.ModifiedBy = "system";
                    canonicalFavorites.Add(duplicateFavorite);
                    continue;
                }

                canonicalFavorite.IsActive = canonicalFavorite.IsActive || duplicateFavorite.IsActive;
                canonicalFavorite.ModifiedBy = "system";
                db.MenuFavorite.Remove(duplicateFavorite);
            }

            db.AppMenu.Remove(duplicate);
        }

        private static string MergeAccessTypes(params string?[] values)
        {
            return string.Join(",", values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string? value) => (value ?? string.Empty).Trim();

        private static async Task AddMenuIconsAsync(ApplicationDbContext db)
        {
            var menuIconsByName = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Home"] = FeatherIcon("home"),
                ["FEATURES"] = FeatherIcon("grid"),
                ["Dashboard"] = FeatherIcon("pie-chart"),
                ["SETTINGS"] = FeatherIcon("settings"),
                ["Master Data"] = FeatherIcon("database"),
                ["Configs"] = FeatherIcon("sliders"),
                ["API"] = FeatherIcon("shield"),
                ["Livestocks"] = FeatherIcon("repeat"),
                ["Farm Transactions"] = FeatherIcon("repeat"),
                ["Farms"] = FeatherIcon("map-pin"),
                ["Logs"] = FeatherIcon("activity"),
                ["Livestock Reports"] = FeatherIcon("bar-chart-2"),
                ["Reports"] = FeatherIcon("bar-chart-2")
            };

            var menuIconsByController = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Home"] = FeatherIcon("home"),
                ["Dashboard"] = FeatherIcon("pie-chart"),
                ["Farm"] = FeatherIcon("map-pin"),
                ["Livestock"] = FeatherIcon("repeat"),
                ["SystemConfig"] = FeatherIcon("settings"),
                ["News"] = FeatherIcon("file-text"),
                ["SourceVideo"] = FeatherIcon("video")
            };

            var menus = await db.AppMenu.AsTracking()
                .Where(x => x.FkParentId == null || x.FkParentId == Guid.Empty)
                .ToListAsync();

            foreach (var menu in menus)
            {
                if (menu.IsSection || !string.IsNullOrWhiteSpace(menu.Icon) || !string.IsNullOrWhiteSpace(menu.IconActive))
                {
                    continue;
                }

                var normalizedName = (menu.Name ?? string.Empty).Trim();
                var selectedIcon = menuIconsByName.TryGetValue(normalizedName, out var iconByName)
                    ? iconByName
                    : null;

                if (selectedIcon == null && !string.IsNullOrWhiteSpace(menu.Controller))
                {
                    var normalizedController = menu.Controller.Trim();
                    if (menuIconsByController.TryGetValue(normalizedController, out var iconByController))
                    {
                        selectedIcon = iconByController;
                    }
                }

                if (selectedIcon == null)
                {
                    continue;
                }

                menu.Icon = selectedIcon;
                menu.IconActive = selectedIcon;
                menu.ModifiedBy = "system";
            }

            await db.SaveChangesAsync();
        }

        private static string FeatherIcon(string name)
        {
            const string start = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">";
            const string end = "</svg>";

            var content = name switch
            {
                "home" => "<path d=\"M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z\"/><polyline points=\"9 22 9 12 15 12 15 22\"/>",
                "grid" => "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\"/>",
                "pie-chart" => "<path d=\"M21.21 15.89A10 10 0 1 1 8 2.83\"/><path d=\"M22 12A10 10 0 0 0 12 2v10z\"/>",
                "settings" => "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z\"/>",
                "database" => "<ellipse cx=\"12\" cy=\"5\" rx=\"9\" ry=\"3\"/><path d=\"M21 12c0 1.66-4 3-9 3s-9-1.34-9-3\"/><path d=\"M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5\"/>",
                "sliders" => "<line x1=\"4\" y1=\"21\" x2=\"4\" y2=\"14\"/><line x1=\"4\" y1=\"10\" x2=\"4\" y2=\"3\"/><line x1=\"12\" y1=\"21\" x2=\"12\" y2=\"12\"/><line x1=\"12\" y1=\"8\" x2=\"12\" y2=\"3\"/><line x1=\"20\" y1=\"21\" x2=\"20\" y2=\"16\"/><line x1=\"20\" y1=\"12\" x2=\"20\" y2=\"3\"/><line x1=\"1\" y1=\"14\" x2=\"7\" y2=\"14\"/><line x1=\"9\" y1=\"8\" x2=\"15\" y2=\"8\"/><line x1=\"17\" y1=\"16\" x2=\"23\" y2=\"16\"/>",
                "shield" => "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/>",
                "user" => "<path d=\"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/>",
                "users" => "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>",
                "menu" => "<line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"/>",
                "user-check" => "<path d=\"M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"8.5\" cy=\"7\" r=\"4\"/><polyline points=\"17 11 19 13 23 9\"/>",
                "mail" => "<path d=\"M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z\"/><polyline points=\"22,6 12,13 2,6\"/>",
                "monitor" => "<rect x=\"2\" y=\"3\" width=\"20\" height=\"14\" rx=\"2\" ry=\"2\"/><line x1=\"8\" y1=\"21\" x2=\"16\" y2=\"21\"/><line x1=\"12\" y1=\"17\" x2=\"12\" y2=\"21\"/>",
                "key" => "<path d=\"M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4\"/>",
                "link" => "<path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\"/><path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\"/>",
                "map-pin" => "<path d=\"M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>",
                "archive" => "<polyline points=\"21 8 21 21 3 21 3 8\"/><rect x=\"1\" y=\"3\" width=\"22\" height=\"5\"/><line x1=\"10\" y1=\"12\" x2=\"14\" y2=\"12\"/>",
                "heart" => "<path d=\"M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z\"/>",
                "message-square" => "<path d=\"M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z\"/>",
                "file-text" => "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"/><polyline points=\"14 2 14 8 20 8\"/><line x1=\"16\" y1=\"13\" x2=\"8\" y2=\"13\"/><line x1=\"16\" y1=\"17\" x2=\"8\" y2=\"17\"/><polyline points=\"10 9 9 9 8 9\"/>",
                "video" => "<polygon points=\"23 7 16 12 23 17 23 7\"/><rect x=\"1\" y=\"5\" width=\"15\" height=\"14\" rx=\"2\" ry=\"2\"/>",
                "repeat" => "<polyline points=\"17 1 21 5 17 9\"/><path d=\"M3 11V9a4 4 0 0 1 4-4h14\"/><polyline points=\"7 23 3 19 7 15\"/><path d=\"M21 13v2a4 4 0 0 1-4 4H3\"/>",
                "clock" => "<circle cx=\"12\" cy=\"12\" r=\"10\"/><polyline points=\"12 6 12 12 16 14\"/>",
                "trending-up" => "<polyline points=\"23 6 13.5 15.5 8.5 10.5 1 18\"/><polyline points=\"17 6 23 6 23 12\"/>",
                "wifi" => "<path d=\"M5 12.55a11 11 0 0 1 14.08 0\"/><path d=\"M1.42 9a16 16 0 0 1 21.16 0\"/><path d=\"M8.53 16.11a6 6 0 0 1 6.95 0\"/><line x1=\"12\" y1=\"20\" x2=\"12.01\" y2=\"20\"/>",
                "terminal" => "<polyline points=\"4 17 10 11 4 5\"/><line x1=\"12\" y1=\"19\" x2=\"20\" y2=\"19\"/>",
                "bell" => "<path d=\"M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9\"/><path d=\"M13.73 21a2 2 0 0 1-3.46 0\"/>",
                "clipboard" => "<path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/><rect x=\"8\" y=\"2\" width=\"8\" height=\"4\" rx=\"1\" ry=\"1\"/>",
                "bar-chart-2" => "<line x1=\"18\" y1=\"20\" x2=\"18\" y2=\"10\"/><line x1=\"12\" y1=\"20\" x2=\"12\" y2=\"4\"/><line x1=\"6\" y1=\"20\" x2=\"6\" y2=\"14\"/>",
                "activity" => "<polyline points=\"22 12 18 12 15 21 9 3 6 12 2 12\"/>",
                "bar-chart" => "<line x1=\"12\" y1=\"20\" x2=\"12\" y2=\"10\"/><line x1=\"18\" y1=\"20\" x2=\"18\" y2=\"4\"/><line x1=\"6\" y1=\"20\" x2=\"6\" y2=\"16\"/>",
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            };

            return $"{start}{content}{end}";
        }

        private static async Task AddSuperAdminAsync(ApplicationDbContext db)
        {
            var role = await db.AppRole.AsTracking().FirstAsync(x => x.Name == "SUPER.ADMIN");
            var user = await db.UserLogin.AsTracking().FirstOrDefaultAsync(x => x.Username == "SuperAdmin");
            if (user == null)
            {
                user = new UserLogin
                {
                    Username = "SuperAdmin", Password = LocalPasswordHasher.Hash("12345678"),
                    Email = "superadmin@smartsheep.local", FullName = "Super Administrator",
                    HP = "-", CreatedBy = "system", ModifiedBy = "system", IsActive = true
                };
                db.UserLogin.Add(user);
                await db.SaveChangesAsync();
            }
            else
            {
                user.Email = "superadmin@smartsheep.local";
                user.FullName = "Super Administrator";
                user.HP = "-";
                user.ModifiedBy = "system";
                user.IsActive = true;
            }

            var userRole = await db.UserLoginRole.AsTracking().FirstOrDefaultAsync(x => x.FkUserId == user.Id && x.FkRoleId == role.Id);
            if (userRole == null)
            {
                db.UserLoginRole.Add(new UserLoginRole { FkUserId = user.Id, FkRoleId = role.Id, CreatedBy = "system", ModifiedBy = "system", IsActive = true });
            }
            else
            {
                userRole.ModifiedBy = "system";
                userRole.IsActive = true;
            }
            await db.SaveChangesAsync();
        }

        private static async Task AddLocalUserMenusAsync(ApplicationDbContext db)
        {
            var configMenu = await db.AppMenu.AsTracking().FirstAsync(x => x.Name == "Configs");
            var apiMenu = await db.AppMenu.AsTracking().FirstAsync(x => x.Name == "API");
            var superAdminRole = await db.AppRole.AsTracking().FirstAsync(x => x.Name == "SUPER.ADMIN");

            var menuDefinitions = new[]
            {
                new { ParentId = configMenu.Id, Name = "User Login", Controller = "UserLogin", Sequence = "c.2.4", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = configMenu.Id, Name = "User Login Role", Controller = "UserLoginRole", Sequence = "c.2.5", AccessTypes = "view,add,save" },
                new { ParentId = configMenu.Id, Name = "User Farms", Controller = "UserFarm", Sequence = "c.2.5.1", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = configMenu.Id, Name = "App Role", Controller = "AppRole", Sequence = "c.2.6", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = configMenu.Id, Name = "App Menu", Controller = "AppMenu", Sequence = "c.2.7", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = configMenu.Id, Name = "App Menu Role", Controller = "AppMenuRole", Sequence = "c.2.8", AccessTypes = "view,add,save" },
                new { ParentId = configMenu.Id, Name = "Email Template", Controller = "EmailTemplate", Sequence = "c.2.9", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = apiMenu.Id, Name = "App Clients", Controller = "AppClient", Sequence = "c.3.1", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = apiMenu.Id, Name = "Scope Clients", Controller = "ScopeClient", Sequence = "c.3.2", AccessTypes = "view,add,edit,delete,detail" },
                new { ParentId = apiMenu.Id, Name = "App Scope Clients", Controller = "AppScopeClient", Sequence = "c.3.3", AccessTypes = "view,add,save" }
            };

            foreach (var definition in menuDefinitions)
            {
                var menu = await db.AppMenu.AsTracking().FirstOrDefaultAsync(x => x.Controller == definition.Controller);
                if (menu == null)
                {
                    menu = new AppMenu
                    {
                        FkParentId = definition.ParentId,
                        Name = definition.Name,
                        Controller = definition.Controller,
                        SequenceNumber = definition.Sequence,
                        AccessTypes = definition.AccessTypes,
                        IsSection = false,
                        IsMobile = definition.Controller != "UserFarm",
                        Icon = null,
                        IconActive = null,
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        IsActive = true
                    };
                    db.AppMenu.Add(menu);
                    await db.SaveChangesAsync();
                }
                else
                {
                    menu.FkParentId = definition.ParentId;
                    menu.Name = definition.Name;
                    menu.SequenceNumber = definition.Sequence;
                    menu.AccessTypes = definition.AccessTypes;
                    menu.IsSection = false;
                    menu.ModifiedBy = "system";
                    menu.IsActive = true;
                    await db.SaveChangesAsync();
                }

                var menuRole = await db.AppMenuRole.AsTracking().FirstOrDefaultAsync(x => x.FkAppMenuId == menu.Id && x.FkAppRoleId == superAdminRole.Id);
                if (menuRole == null)
                {
                    db.AppMenuRole.Add(new AppMenuRole
                    {
                        FkAppMenuId = menu.Id,
                        FkAppRoleId = superAdminRole.Id,
                        AccessTypes = menu.AccessTypes,
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        IsActive = true
                    });
                    await db.SaveChangesAsync();
                }
                else
                {
                    menuRole.AccessTypes = definition.AccessTypes;
                    menuRole.ModifiedBy = "system";
                    menuRole.IsActive = true;
                    await db.SaveChangesAsync();
                }
            }
        }

        private static async Task AddSmartSheepDataAndMenusAsync(ApplicationDbContext db)
        {
            var role = await db.AppRole.AsTracking().FirstAsync(x => x.Name == "SUPER.ADMIN");
            var masterParent = await db.AppMenu.AsTracking().FirstAsync(x => x.Name == "Master Data");
            var configParent = await db.AppMenu.AsTracking().FirstAsync(x => x.Name == "Configs");

            async Task<AppMenu> EnsureMenu(string name, string? controller, string sequence, Guid? parentId, string access = "view,add,edit,delete,detail")
            {
                var menu = await db.AppMenu.AsTracking().FirstOrDefaultAsync(x => x.Name == name && x.Controller == controller);
                if (menu == null)
                {
                    menu = new AppMenu { Name = name, Controller = controller, SequenceNumber = sequence, FkParentId = parentId, AccessTypes = access, IsSection = false, IsMobile = true, CreatedBy = "system", ModifiedBy = "system", IsActive = true };
                    var iconName = controller switch
                    {
                        "SucklingActivity" => "activity",
                        "SucklingStatistic" => "bar-chart-2",
                        "SheepBreeding" => "heart",
                        _ => null
                    };
                    if (iconName != null) menu.Icon = menu.IconActive = FeatherIcon(iconName);
                    db.AppMenu.Add(menu);
                    await db.SaveChangesAsync();
                }
                else
                {
                    menu.Name = name;
                    menu.Controller = controller;
                    menu.SequenceNumber = sequence;
                    menu.FkParentId = parentId;
                    menu.AccessTypes = access;
                    menu.IsSection = false;
                    menu.ModifiedBy = "system";
                    menu.IsActive = true;
                    await db.SaveChangesAsync();
                }

                var menuRole = await db.AppMenuRole.AsTracking().FirstOrDefaultAsync(x => x.FkAppMenuId == menu.Id && x.FkAppRoleId == role.Id);
                if (menuRole == null)
                {
                    db.AppMenuRole.Add(new AppMenuRole { FkAppMenuId = menu.Id, FkAppRoleId = role.Id, AccessTypes = access, CreatedBy = "system", ModifiedBy = "system", IsActive = true });
                    await db.SaveChangesAsync();
                }
                else
                {
                    menuRole.AccessTypes = access;
                    menuRole.ModifiedBy = "system";
                    menuRole.IsActive = true;
                    await db.SaveChangesAsync();
                }
                return menu;
            }

            await EnsureMenu("Farms", "Farm", "c.1.10", masterParent.Id);
            await EnsureMenu("Barns", "Barn", "c.1.11", masterParent.Id);
            await EnsureMenu("Livestock", "Livestock", "c.1.12", masterParent.Id);
            await EnsureMenu("System Config", "SystemConfig", "c.2.10", configParent.Id);
            await EnsureMenu("Reminder Templates", "ReminderTemplate", "c.2.11", configParent.Id);
            await EnsureMenu("News", "News", "c.2.12", configParent.Id);
            await EnsureMenu("Source Videos", "SourceVideo", "c.1.13", masterParent.Id);

            var transactionParent = await EnsureMenu("Livestocks", null, "b.10", null, "view");
            await EnsureMenu("Suckling Activities", "SucklingActivity", "b.10.1", transactionParent.Id);
            await EnsureMenu("Suckling Statistics", "SucklingStatistic", "b.10.2", transactionParent.Id);
            await EnsureMenu("Sheep Breeding", "SheepBreeding", "b.10.3", transactionParent.Id, "view");

            var logParent = await EnsureMenu("Logs", null, "b.11", null, "view");
            await EnsureMenu("Source Video Status Logs", "SourceVideoStatusLog", "b.11.1", logParent.Id, "view,add,detail");
            await EnsureMenu("Job Execution Logs", "JobExecutionLog", "b.11.2", logParent.Id, "view,add,detail");
            await EnsureMenu("Reminder Logs", "ReminderLog", "b.11.3", logParent.Id, "view,add,edit,delete,detail");

            var reportParent = await EnsureMenu("Reports", null, "b.12", null, "view");
            await EnsureMenu("Suckling Activity Report", "SucklingActivityReport", "b.12.1", reportParent.Id, "view");
            await EnsureMenu("Suckling Statistic Report", "SucklingStatisticReport", "b.12.2", reportParent.Id, "view");
            await EnsureMenu("Reminder Report", "ReminderReport", "b.12.3", reportParent.Id, "view");
        }

        private static async Task AddSmartSheepDataAsync(ApplicationDbContext db)
        {
            var template = await db.ReminderTemplate.FirstOrDefaultAsync();
            if (template == null)
            {
                template = new ReminderTemplate { Id = Guid.NewGuid(), Code = "LOW-SUCKLING", Name = "Low Suckling Activity", ReminderText = "Suckling activity is below the expected threshold. Please check the barn and sheep condition.", CreatedBy = "system", ModifiedBy = "system", DateCreated = DateTime.Now, DateModified = DateTime.Now, IsActive = true };
                db.ReminderTemplate.Add(template);
            }

            var systemConfig = await db.SystemConfig.FirstOrDefaultAsync();
            if (systemConfig == null)
            {
                db.SystemConfig.Add(new SystemConfig { Id = Guid.NewGuid(), StatisticsDurationMinutes = 120, SourceVideoHealthCheckIntervalMinutes = 5, CreatedBy = "system", ModifiedBy = "system", DateCreated = DateTime.Now, DateModified = DateTime.Now, IsActive = true });
            }
            await db.SaveChangesAsync();

            // Sample data is one related graph. Never append it to an existing
            // operational dataset, including inactive records.
            if (await db.Farm.AnyAsync() || await db.Barn.AnyAsync() ||
                await db.Livestock.AnyAsync() || await db.SourceVideo.AnyAsync() ||
                await db.SucklingActivity.AnyAsync() || await db.SucklingStatistic.AnyAsync() ||
                await db.ReminderLog.AnyAsync() || await db.SourceVideoStatusLog.AnyAsync() ||
                await db.JobExecutionLog.AnyAsync())
            {
                return;
            }

            {
                var now = DateTime.Now;
                var today = DateTime.Today;
                var random = new Random(42);

                var farms = new[]
                {
                    new { Code = "FRM-JKT-01", Name = "Cikarawang Farm", Location = "Dramaga, Bogor", Address = "Kampung Cikarawang, Dramaga, Bogor", Latitude = "-6.55000000", Longitude = "106.73000000", Region = "West Java", BarnSuffix = "JKT", BarnCount = 2 },
                    new { Code = "FRM-BDG-01", Name = "Bandung Farm", Location = "Coblong, Bandung", Address = "Jl. Dago Atas No. 12, Coblong, Bandung", Latitude = "-6.89500000", Longitude = "107.63300000", Region = "West Java", BarnSuffix = "BDG", BarnCount = 2 },
                    new { Code = "FRM-SBY-01", Name = "Surabaya Farm", Location = "Wonokromo, Surabaya", Address = "Jl. Ngagel No. 7, Wonokromo, Surabaya", Latitude = "-7.26500000", Longitude = "112.73400000", Region = "East Java", BarnSuffix = "SBY", BarnCount = 3 },
                    new { Code = "FRM-MDN-01", Name = "Medan Farm", Location = "Medan Baru, Medan", Address = "Jl. Gatot Subroto, Medan Baru, Medan", Latitude = "3.59700000", Longitude = "98.67800000", Region = "North Sumatra", BarnSuffix = "MDN", BarnCount = 2 },
                    new { Code = "FRM-MKS-01", Name = "Makassar Farm", Location = "Panakkukang, Makassar", Address = "Jl. Sultan Alauddin, Panakkukang, Makassar", Latitude = "-5.13540000", Longitude = "119.42380000", Region = "South Sulawesi", BarnSuffix = "MKS", BarnCount = 2 },
                    new { Code = "FRM-DPS-01", Name = "Denpasar Farm", Location = "Denpasar Selatan, Denpasar", Address = "Jl. Teuku Umar, Denpasar Selatan, Bali", Latitude = "-8.66900000", Longitude = "115.21400000", Region = "Bali", BarnSuffix = "DPS", BarnCount = 2 },
                    new { Code = "FRM-YKG-01", Name = "Yogyakarta Farm", Location = "Sleman, Yogyakarta", Address = "Jl. Kaliurang Km 15, Sleman", Latitude = "-7.79706800", Longitude = "110.37052900", Region = "Yogyakarta", BarnSuffix = "YKG", BarnCount = 3 },
                    new { Code = "FRM-BKS-01", Name = "Pondok Gede Farm", Location = "Pondok Gede, Bekasi", Address = "Jl. Raya Pondok Gede, Bekasi", Latitude = "-6.25340000", Longitude = "106.88520000", Region = "West Java", BarnSuffix = "BKS", BarnCount = 2 }
                };

                foreach (var item in farms)
                {
                    var farm = new Farm
                    {
                        Id = Guid.NewGuid(),
                        Code = item.Code,
                        Name = item.Name,
                        Location = item.Location,
                        Longitude = item.Longitude,
                        Latitude = item.Latitude,
                        Address = item.Address,
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        DateCreated = now,
                        DateModified = now,
                        IsActive = true
                    };
                    db.Farm.Add(farm);

                    for (int barnIndex = 1; barnIndex <= item.BarnCount; barnIndex++)
                    {
                        var sheep = new Livestock
                        {
                            Id = Guid.NewGuid(),
                            Code = $"SHP-{item.BarnSuffix}-{barnIndex:00}",
                            Name = $"{item.Region} Sheep {barnIndex}",
                            Sex = barnIndex % 2 == 0 ? "Male" : "Female",
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };

                        var barn = new Barn
                        {
                            Id = Guid.NewGuid(),
                            Code = $"BRN-{item.BarnSuffix}-{barnIndex:00}",
                            Name = $"{item.Name} Barn {barnIndex}",
                            Capacity = barnIndex % 3 == 0 ? 30 : 20,
                            Length = barnIndex % 3 == 0 ? 18 : 12,
                            Width = barnIndex % 3 == 0 ? 10 : 7,
                            Type = barnIndex % 3 == 0 ? "Large" : "Small",
                            FkFarmId = farm.Id,
                            FkLivestockId = sheep.Id,
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };
                        db.AddRange(sheep, barn);

                        var sourceVideo = new SourceVideo
                        {
                            Id = Guid.NewGuid(),
                            FkBarnId = barn.Id,
                            Code = $"SRC-{item.BarnSuffix}-{barnIndex:00}-01",
                            SourceVideoUrl = "https://www.youtube.com/watch?v=pBFmbzw0cdw",
                            IsOnline = random.NextDouble() >= 0.2,
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };
                        db.SourceVideo.Add(sourceVideo);

                        for (int statusIndex = 0; statusIndex < 7; statusIndex++)
                        {
                            var statusDate = today.AddDays(-statusIndex);
                            var isOnline = (item.Code.GetHashCode() + barnIndex + statusIndex) % 4 != 0;
                            db.SourceVideoStatusLog.Add(new SourceVideoStatusLog
                            {
                                Id = Guid.NewGuid(),
                                FkIdSourceVideo = sourceVideo.Id,
                                Code = $"CHK-{item.BarnSuffix}-{barnIndex:00}-{statusDate:yyyyMMdd}",
                                LoggedAt = statusDate.AddHours(8),
                                Status = isOnline ? "Online" : "Offline",
                                CreatedBy = "system",
                                ModifiedBy = "system",
                                DateCreated = now,
                                DateModified = now,
                                IsActive = true
                            });
                        }

                        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
                        {
                            var day = today.AddDays(-dayOffset);
                            var dayFrequencies = new List<int>();
                            var dayDurations = new List<int>();
                            for (int hour = 6; hour <= 20; hour += 3)
                            {
                                var activityStart = day.AddHours(hour).AddMinutes(random.Next(0, 9));
                                var hourShift = ((hour >= 6 && hour <= 10) || (hour >= 17 && hour <= 20)) ? 1.2 : 0.6;
                                var durationMinutes = Math.Clamp(
                                    (int)(14 + ((Math.Abs(item.Code.GetHashCode()) + barnIndex * 19 + dayOffset * 13 + hour * 11) % 150) * hourShift)
                                        + random.Next(-8, 9),
                                    6,
                                    240);
                                var randomNoise = random.Next(-12, 13);
                                var frequency = Math.Clamp(
                                    (int)(50 + ((Math.Abs(item.Code.GetHashCode()) + barnIndex * 37 + dayOffset * 17 + hour * 8) % 140) * hourShift + randomNoise),
                                    10,
                                    260);

                                db.SucklingActivity.Add(new SucklingActivity
                                {
                                    Id = Guid.NewGuid(),
                                    FkBarnId = barn.Id,
                                    ActivityStart = activityStart,
                                    ActivityEnd = activityStart.AddMinutes(durationMinutes),
                                    TotalFrequency = frequency,
                                    TotalDurationSeconds = durationMinutes * 60,
                                    CreatedBy = "system",
                                    ModifiedBy = "system",
                                    DateCreated = now,
                                    DateModified = now,
                                    IsActive = true
                                });

                                dayFrequencies.Add(frequency);
                                dayDurations.Add(durationMinutes);
                            }

                            var statFrequency = dayFrequencies.Sum();
                            var statDuration = dayDurations.Sum() * 60;
                            var requiresReminder = statFrequency < 420;
                            var statistic = new SucklingStatistic
                            {
                                Id = Guid.NewGuid(),
                                Code = $"STAT-{item.Code}-{day:yyyyMMdd}-{barnIndex:00}",
                                FkBarnId = barn.Id,
                                PeriodDescription = $"{item.Name} daily statistic",
                                TotalFrequency = statFrequency,
                                TotalDurationSeconds = statDuration,
                                RequiresReminder = requiresReminder,
                                ActivityFrom = day.AddHours(6),
                                ActivityTo = day.AddHours(21),
                                HourNumber = 18,
                                CreatedBy = "system",
                                ModifiedBy = "system",
                                DateCreated = now,
                                DateModified = now,
                                IsActive = true
                            };
                            db.SucklingStatistic.Add(statistic);

                            if (requiresReminder)
                            {
                                db.ReminderLog.Add(new ReminderLog
                                {
                                    Id = Guid.NewGuid(),
                                    FkReminderTemplateId = template.Id,
                                    FkSucklingStatisticId = statistic.Id,
                                    Username = "SuperAdmin",
                                    Status = random.NextDouble() >= 0.5 ? "Pending" : "Sent",
                                    ScheduledAt = day.AddHours(20),
                                    SentAt = random.NextDouble() >= 0.5 ? day.AddHours(20).AddMinutes(random.Next(0, 15)) : null,
                                    ErrorMessage = random.NextDouble() >= 0.9 ? "Timeout while sending reminder notification." : null,
                                    CreatedBy = "system",
                                    ModifiedBy = "system",
                                    DateCreated = now,
                                    DateModified = now,
                                    IsActive = true
                                });
                            }
                        }

                        for (int run = 0; run < 3; run++)
                        {
                            var executed = today.AddDays(-run).AddHours(random.Next(1, 23));
                            db.JobExecutionLog.Add(new JobExecutionLog
                            {
                                Id = Guid.NewGuid(),
                                ExecutedAt = executed,
                                Code = "SOURCEVIDEO-HEALTH-CHECK",
                                Status = random.NextDouble() >= 0.15 ? "Succeeded" : "Failed",
                                StatusNotes = random.NextDouble() >= 0.15
                                    ? "Health check completed."
                                    : "Health check failed due to timeout while fetching frame.",
                                CreatedBy = "system",
                                ModifiedBy = "system",
                                DateCreated = now,
                                DateModified = now,
                                IsActive = true
                            });
                        }
                    }
                }

                await db.SaveChangesAsync();
            }

            await AddIndonesiaDashboardSeedAsync(db, template);
        }

        private static async Task AddDetikNewsSeedAsync(ApplicationDbContext db)
        {
            if (await db.News.AnyAsync())
            {
                return;
            }

            var newsItems = new[]
            {
                new { Code = "NEWS-DETIK-001", Title = "Melihat Peternakan Domba di Selandia Baru", Summary = "Withers Farm di Auckland, adalah salah satu lokasi peternakan domba di Selandia Baru. Yuk kita lihat.", Image = "https://awsimages.detik.net.id/api/wm/2023/05/25/melihat-peternakan-domba-di-selandia-baru_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2023-05-26T07:00:27+07:00", SourceUrl = "https://finance.detik.com/foto-bisnis/d-6738756/melihat-peternakan-domba-di-selandia-baru" },
                new { Code = "NEWS-DETIK-002", Title = "Kontes Hewan Ternak Banyuwangi, Sapi-Domba Dandan Buat Jadi yang Terbaik", Summary = "Dinas Pertanian dan Pangan Banyuwangi menggelar acara kontes hewan ternak. Ajang ini sebagai ajang pemasaran hewan kurban jelang Hari Raya Idul Adha.", Image = "https://awsimages.detik.net.id/api/wm/2023/06/20/sapi-domba-percantik-diri-di-kontes-hewan-ternak-banyuwangi-1_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2023-06-20T18:30:57+07:00", SourceUrl = "https://finance.detik.com/foto-bisnis/d-6782827/kontes-hewan-ternak-banyuwangi-sapi-domba-dandan-buat-jadi-yang-terbaik" },
                new { Code = "NEWS-DETIK-003", Title = "Krisis Ekonomi dan Kekeringan Bikin Harga Domba Meroket di Tunisia", Summary = "Jelang Idul Adha harga domba di Tunisia melambung naik. Hal itu disebabkan krisis ekonomi hingga kekeringan dan mahalnya pakan ternak.", Image = "https://awsimages.detik.net.id/api/wm/2023/06/21/tunisia-inflationsheep_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2023-06-21T22:05:24+07:00", SourceUrl = "https://finance.detik.com/foto-bisnis/d-6785090/krisis-ekonomi-dan-kekeringan-bikin-harga-domba-meroket-di-tunisia" },
                new { Code = "NEWS-DETIK-004", Title = "Lihat Langsung Peternakan Kambing Perah di Banyuwangi", Summary = "Sebanyak 350 ekor kambing perah jenis Etawa, PE dan Sapera yang diternakan di Sentra peternakan kambing Karya etawa di Kalipuro, Banyuwangi. Yuk lihat.", Image = "https://awsimages.detik.net.id/api/wm/2025/02/12/lihat-langsung-peternakan-kambing-perah-di-banyuwangi_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-02-12T21:00:53+07:00", SourceUrl = "https://finance.detik.com/foto-bisnis/d-7775048/lihat-langsung-peternakan-kambing-perah-di-banyuwangi" },
                new { Code = "NEWS-DETIK-005", Title = "Setengah Abad Eksis, Sate Domba Garut Matuh Terancam Tak Ada Penerus", Summary = "Sate Domba Garut Matuh, kuliner yang layak dicoba saat menyambangi Garut. Kelezatan sate di sini sudah ditawarkan selama 50 tahun!", Image = "https://awsimages.detik.net.id/api/wm/2025/09/11/sate-domba-garut-matuh-1757563542692_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-11T15:00:40+07:00", SourceUrl = "https://food.detik.com/berita-boga/d-8106627/setengah-abad-eksis-sate-domba-garut-matuh-terancam-tak-ada-penerus" },
                new { Code = "NEWS-DETIK-006", Title = "Momen Seru Raffi Ahmad dan Irfan Hakim Main ke Kandang Domba Nusakambangan", Summary = "Raffi lalu bertanya kepada Irfan Hakim soal usia anak domba. Saat inilah momen lucu terjadi, Irfan Hakim menjawab dengan bercanda.", Image = "https://akcdn.detik.net.id/api/wm/2025/11/11/keseruan-irfan-hakim-raffi-ahmad-dan-menteri-imipas-agus-andrianto-di-peternakan-domba-garut-lapas-kelas-iia-kembang-kuning-nu-1762866581698_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-11-11T20:12:48+07:00", SourceUrl = "https://news.detik.com/berita/d-8206085/momen-seru-raffi-ahmad-dan-irfan-hakim-main-ke-kandang-domba-nusakambangan" },
                new { Code = "NEWS-DETIK-007", Title = "Barantin Antisipasi Virus PPR, Arus Masuk Kambing dan Domba Kini Diperketat", Summary = "Badan Karantina Indonesia memperketat arus masuk kambing dan domba untuk mencegah virus PPR. Edukasi dan koordinasi dilakukan untuk melindungi peternak.", Image = "https://awsimages.detik.net.id/api/wm/2026/01/15/konpers-barantin-1768467719110_169.png?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-01-15T16:02:12+07:00", SourceUrl = "https://news.detik.com/berita/d-8309111/barantin-antisipasi-virus-ppr-arus-masuk-kambing-dan-domba-kini-diperketat" },
                new { Code = "NEWS-DETIK-008", Title = "Panduan Memilih Hewan Kurban untuk Idul Adha 2026", Summary = "Simak panduan memilih hewan kurban untuk Idul Adha 2026 dari Ditjen PKH Kementan RI.", Image = "https://awsimages.detik.net.id/api/wm/2020/07/16/wagub-dki-pantau-hewan-kurban-2_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-13T08:25:48+07:00", SourceUrl = "https://news.detik.com/berita/d-8486322/panduan-memilih-hewan-kurban-untuk-idul-adha-2026" },
                new { Code = "NEWS-DETIK-009", Title = "Peternakan di Bogor Terbakar Dini Hari, Ribuan Anak Ayam dan Belasan Domba Mati", Summary = "Kebakaran hebat melanda peternakan ayam di Cariu, Bogor, mengakibatkan ribuan anak ayam dan domba mati. Kerugian diperkirakan mencapai Rp 500 juta.", Image = "https://akcdn.detik.net.id/api/wm/2026/05/19/peternakan-di-cariu-bogor-terbakar-mengakibatkan-ribuan-anak-ayam-hingga-belasan-domba-mati-terpanggang-pada-selasa-1952026-di-1779177184001_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-19T14:56:59+07:00", SourceUrl = "https://news.detik.com/berita/d-8495197/peternakan-di-bogor-terbakar-dini-hari-ribuan-anak-ayam-dan-belasan-domba-mati" },
                new { Code = "NEWS-DETIK-010", Title = "Stok Hewan Kurban di Jabar untuk Idul Adha 2026 Dipastikan Aman", Summary = "Ketersediaan hewan kurban di Jawa Barat untuk Iduladha 2026 aman, dengan stok domba, sapi, dan kerbau meningkat. Pengawasan kesehatan hewan dilakukan ketat.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/08/spg-pramugari-jadi-daya-tarik-mal-hewan-kurban-depok-1778221026154_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-20T17:12:17+07:00", SourceUrl = "https://news.detik.com/berita/d-8497213/stok-hewan-kurban-di-jabar-untuk-idul-adha-2026-dipastikan-aman" },
                new { Code = "NEWS-DETIK-011", Title = "Masjid Raya Pondok Indah Jaksel Terima Kurban 10 Sapi dan 35 Kambing", Summary = "Masjid Raya Pondok Indah, Jakarta Selatan, menerima sebanyak 10 sapi dan 35 kambing pada kurban hari Idul Adha 1447 H.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/27/masjid-raya-pondok-indah-jakarta-selatan-memotong-sebanyak-10-sapi-dan-35-kambing-pada-kurban-hari-idul-adha-1447-h-adhfardeti-1779856905157_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-27T11:48:05+07:00", SourceUrl = "https://news.detik.com/berita/d-8507007/masjid-raya-pondok-indah-jaksel-terima-kurban-10-sapi-dan-35-kambing" },
                new { Code = "NEWS-DETIK-012", Title = "Napi di Lapas Ciangir Dilatih Urus Ternak, Produknya Sampai Disuplai ke Hotel", Summary = "Lapas Kelas IIB Ciangir memberdayakan narapidana dengan peternakan dan pertanian. Warga binaan dapat upah dari hasil kerja.", Image = "https://awsimages.detik.net.id/api/wm/2026/07/09/lapas-ciangir-1783584782704_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-07-09T15:19:33+07:00", SourceUrl = "https://news.detik.com/berita/d-8566833/napi-di-lapas-ciangir-dilatih-urus-ternak-produknya-sampai-disuplai-ke-hotel" },
                new { Code = "NEWS-DETIK-013", Title = "Napi Raup Rp 800 Ribu Per Bulan dari Hasil Urus Ternak di Lapas Ciangir", Summary = "Warga binaan Lapas Ciangir diberdayakan melalui peternakan dan pertanian. Mereka mendapatkan upah bulanan dan belajar keterampilan baru.", Image = "https://awsimages.detik.net.id/api/wm/2026/07/09/lapas-ciangir-1783584941275_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-07-09T15:41:05+07:00", SourceUrl = "https://news.detik.com/berita/d-8566882/napi-raup-rp-800-ribu-per-bulan-dari-hasil-urus-ternak-di-lapas-ciangir" },
                new { Code = "NEWS-DETIK-014", Title = "Melihat Wisata Edukasi Peternakan Kambing di Wonosobo Jawa Tengah", Summary = "Peternakan kambing dan domba Alisan Farm di Wonosobo, Jateng, jadi wisata edukasi bagi anak-anak. Peternakan ini menghasilkan susu dan daging kambing.", Image = "https://awsimages.detik.net.id/api/wm/2024/05/14/melihat-wisata-edukasi-peternakan-kambing-di-wonosobo-jawa-tengah-6_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2024-05-15T07:01:06+07:00", SourceUrl = "https://travel.detik.com/fototravel/d-7339867/melihat-wisata-edukasi-peternakan-kambing-di-wonosobo-jawa-tengah" },
                new { Code = "NEWS-DETIK-015", Title = "Kontes Domba Batur Semarakkan Dieng Culture Festival 2026", Summary = "Kontes Domba Batur menjadi salah satu daya tarik Dieng Culture Festival 2026 di Banjarnegara. Ratusan peserta turut mengenalkan potensi ternak unggulan daerah.", Image = "https://awsimages.detik.net.id/api/wm/2026/08/29/kontes-domba-batur-semarakkan-dieng-culture-festival-2026-1788006837030_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-08-30T19:30:23+07:00", SourceUrl = "https://travel.detik.com/fototravel/d-8639837/kontes-domba-batur-semarakkan-dieng-culture-festival-2026" },
                new { Code = "NEWS-DETIK-016", Title = "Serunya Nonton Adu Ketangkasan Domba Garut Piala Presiden 2025", Summary = "Adu ketangkasan Domba Garut Piala Presiden 2025 digelar di Pakansari, ratusan peternak ikut serta, warga pun antusias menyaksikan tradisi ini.", Image = "https://awsimages.detik.net.id/api/wm/2025/09/20/adu-ketangkasan-domba-garut-piala-presiden-2025-1758354839173_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-20T18:00:00+07:00", SourceUrl = "https://travel.detik.com/fototravel/d-8121732/serunya-nonton-adu-ketangkasan-domba-garut-piala-presiden-2025" },
                new { Code = "NEWS-DETIK-017", Title = "Suara Embikan Domba Bongkar Penyamaran Maling di Cikakak Sukabumi", Summary = "Komplotan pencuri hewan ternak di Sukabumi tertangkap setelah mobil mereka pecah ban. Warga curiga saat mendengar suara domba dari dalam mobil.", Image = "https://awsimages.detik.net.id/api/wm/2026/06/03/ilustrasi-pencurian-domba-di-sukabumi-1780462992805_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-06-03T13:30:50+07:00", SourceUrl = "https://www.detik.com/jabar/hukum-dan-kriminal/d-8515789/suara-embikan-domba-bongkar-penyamaran-maling-di-cikakak-sukabumi" },
                new { Code = "NEWS-DETIK-018", Title = "Perjuangan Siham, Mahasiswa Autis Lulus Fapet UGM dan Rintis Usaha Ternak Domba", Summary = "Siham, mahasiswa autis dari UGM, lulus setelah 6 tahun 7 bulan dan kini merintis usaha ternak domba. Kisahnya inspiratif bagi semua orang.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/21/siham-mahasiswa-autis-yang-lulus-fapet-ugm-1779370159079_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-22T08:00:00+07:00", SourceUrl = "https://www.detik.com/edu/edutainment/d-8499131/perjuangan-siham-mahasiswa-autis-lulus-fapet-ugm-dan-rintis-usaha-ternak-domba" },
                new { Code = "NEWS-DETIK-019", Title = "Lulusan Peternakan Jadi Apa? Mila Dirikan Usaha Beromzet Rp 50 Juta per Bulan", Summary = "Mila Arlinda, lulusan Fapet UGM, sukses mendirikan usaha ternak beromzet puluhan juta per bulan. Ini kisahnya.", Image = "https://awsimages.detik.net.id/api/wm/2026/06/29/mila-arlinda-lulusan-fapet-ugm-sukses-mendirikan-usaha-ternak-beromzet-puluhan-juta-per-bulan-1782730403662_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-07-01T09:00:00+07:00", SourceUrl = "https://www.detik.com/edu/edutainment/d-8552881/lulusan-peternakan-jadi-apa-mila-dirikan-usaha-beromzet-rp-50-juta-per-bulan" },
                new { Code = "NEWS-DETIK-020", Title = "Inovasi Smart Farming Jadi Sarana Edukasi Peternakan", Summary = "Teknologi Smart Farming dengan sistem RFID diterapkan untuk peternakan kambing. Program ini juga menjadi sarana belajar praktis bagi peternak dan mahasiswa.", Image = "https://awsimages.detik.net.id/api/wm/2025/09/17/peternakan-kambing-naik-kelas-berkat-smart-farming-1758080812278_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-17T10:30:24+07:00", SourceUrl = "https://www.detik.com/edu/foto/d-8115981/inovasi-smart-farming-jadi-sarana-edukasi-peternakan" },
                new { Code = "NEWS-DETIK-021", Title = "Berapa Minimal Usia Domba, Kambing dan Sapi yang Sah untuk Kurban?", Summary = "Berapa umur minimal domba, kambing, dan sapi untuk kurban? Simak syarat usia hewan kurban agar ibadah sah sesuai ketentuan syariat Islam.", Image = "https://awsimages.detik.net.id/api/wm/2025/06/07/njagani-jateng-1749292195493_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-04-04T08:00:19+07:00", SourceUrl = "https://www.detik.com/hikmah/khazanah/d-8428094/berapa-minimal-usia-domba-kambing-dan-sapi-yang-sah-untuk-kurban" },
                new { Code = "NEWS-DETIK-022", Title = "Mau Kurban Kambing untuk Satu Keluarga? Begini Aturan Syariatnya", Summary = "Kurban kambing satu keluarga sah secara syariat. Pelajari hukumnya lengkap dan cara berkurban kambing mulai Rp 1 jutaan bersama Dompet Dhuafa.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/25/dompet-dhuafa-1779696353718_169.webp?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-25T15:18:06+07:00", SourceUrl = "https://www.detik.com/hikmah/khazanah/d-8504165/mau-kurban-kambing-untuk-satu-keluarga-begini-aturan-syariatnya" },
                new { Code = "NEWS-DETIK-023", Title = "Ketentuan Jumlah Orang yang Kurban Satu Ekor Kambing, Bolehkah untuk Sekeluarga?", Summary = "Menjelang Idul Adha, pertanyaan jumlah orang yang kurban satu ekor kambing kerap ditanyakan. Bagaimana ketentuannya dalam fikih?", Image = "https://awsimages.detik.net.id/api/wm/2026/05/05/ilustrasi-kambing-kurban-1777952225607_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-26T07:15:52+07:00", SourceUrl = "https://www.detik.com/hikmah/khazanah/d-8504752/ketentuan-jumlah-orang-yang-kurban-satu-ekor-kambing-bolehkah-untuk-sekeluarga" },
                new { Code = "NEWS-DETIK-024", Title = "Kode dari TikTok Berakhir Bahagia Wilujeng Dapat Seserahan Domba Merino", Summary = "Wilujeng terkejut saat suaminya, Malindo, memberikan domba Merino sebagai seserahan pernikahan. Domba idamannya kini akan dirawat dan dikembangbiakkan.", Image = "https://awsimages.detik.net.id/api/wm/2026/08/09/domba-merino-jadi-seserahan-pernikahan-1786263650058_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-08-09T18:00:12+07:00", SourceUrl = "https://www.detik.com/jatim/berita/d-8611013/kode-dari-tiktok-berakhir-bahagia-wilujeng-dapat-seserahan-domba-merino" },
                new { Code = "NEWS-DETIK-025", Title = "Pesta Patok Tasik, Ajang Adu Gagah yang Bisa Bikin Harga Domba Naik", Summary = "Ratusan peternak domba dan kambing meramaikan Pesta Patok 2025 di Tasikmalaya. Kontes ini jadi ajang silaturahmi dan promosi ekonomi bagi peternak lokal.", Image = "https://awsimages.detik.net.id/api/wm/2025/06/26/pesta-patok-2025-di-tasikmalaya-1750913739886_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-06-26T16:00:31+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-7983093/pesta-patok-tasik-ajang-adu-gagah-yang-bisa-bikin-harga-domba-naik" },
                new { Code = "NEWS-DETIK-026", Title = "Wamentan Puji Peringatan Hari Peternakan-Kesehatan Hewan Kabupaten Bogor", Summary = "Wamentan Sudaryono apresiasi Peringatan Hari Peternakan ke-189 di Bogor. Acara ini dorong ketahanan pangan dan inovasi sektor peternakan Indonesia.", Image = "https://awsimages.detik.net.id/api/wm/2025/09/21/pemkab-bogor-1758462117612_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-21T20:42:10+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8123433/wamentan-puji-peringatan-hari-peternakan-kesehatan-hewan-kabupaten-bogor" },
                new { Code = "NEWS-DETIK-027", Title = "Peringatan Hari Peternakan dan Kesehatan Hewan di Bogor, Ada Kontes Domba", Summary = "Pemkab Bogor dan Kementerian Pertanian merayakan Hari Peternakan ke-189 dengan berbagai kegiatan, termasuk penghargaan untuk peternak dan inovasi baru.", Image = "https://akcdn.detik.net.id/api/wm/2025/09/21/pemkab-bogor-1758462371831_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-21T21:10:40+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8123449/peringatan-hari-peternakan-dan-kesehatan-hewan-di-bogor-ada-kontes-domba" },
                new { Code = "NEWS-DETIK-028", Title = "Kemendes PDT Bidik Pengembangan Desa Domba di Karawang", Summary = "Kementerian Desa luncurkan program Desa Domba di Karawang, menggabungkan peternakan modern dan kearifan lokal.", Image = "https://awsimages.detik.net.id/api/wm/2026/02/15/mendes-pdt-yandri-susanto-saat-meninjau-projek-desa-domba-di-karawang-1771169846160_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-02-15T22:37:47+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8357598/kemendes-pdt-bidik-pengembangan-desa-domba-di-karawang" },
                new { Code = "NEWS-DETIK-029", Title = "Ribuan Hewan Kurban di KBB Diperiksa, Bupati: Beli yang Berlabel Sehat", Summary = "Bupati Bandung Barat, Jeje Ritchie, memeriksa kesehatan 2.829 hewan kurban menjelang Iduladha. Pastikan hewan berlabel sehat untuk keamanan masyarakat.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/20/bupati-bandung-barat-jeje-ritchie-ismail-pasang-label-sehat-di-hewan-kurban-1779272473421_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-20T23:00:03+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8497238/ribuan-hewan-kurban-di-kbb-diperiksa-bupati-beli-yang-berlabel-sehat" },
                new { Code = "NEWS-DETIK-030", Title = "Tips Memilih Hewan Kurban yang Sehat dan Sesuai Syariat Islam", Summary = "Menjelang Idul Adha, umat Islam antusias memilih hewan kurban. Pastikan hewan memenuhi syarat syariat dan kesehatan agar ibadah sah dan daging aman.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/08/ilustrasi-sapi-sapi-kurban-di-ud-segar-farm-pekuncen-wirobrajan-kota-jogja-jumat-852026-1778227204341_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-25T06:30:51+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8502671/tips-memilih-hewan-kurban-yang-sehat-dan-sesuai-syariat-islam" },
                new { Code = "NEWS-DETIK-031", Title = "Hewan Kurban di Bandung Mulai Diperiksa, DKPP: Belum Ada Penyakit", Summary = "DKPP Kota Bandung telah memeriksa 13 ribu hewan kurban menjelang Idul Adha. Ratusan petugas dikerahkan untuk memastikan kesehatan dan kelayakan hewan.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/25/hewan-kurban-yang-dinyatakan-sehat-dipasang-stiker-dari-dkpp-kota-bandung-1779700828076_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-25T20:30:51+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8504330/hewan-kurban-di-bandung-mulai-diperiksa-dkpp-belum-ada-penyakit" },
                new { Code = "NEWS-DETIK-032", Title = "Organ Hati Ratusan Hewan Kurban di Bandung Terinfeksi Cacing", Summary = "Pelaksanaan Idul Adha di Bandung temukan cacing hati pada hewan kurban. DKPP pastikan bagian terinfeksi dipisahkan untuk keamanan pangan masyarakat.", Image = "https://awsimages.detik.net.id/api/wm/2023/06/29/geliat-penyembelihan-hewan-kurban-di-rph-bandung_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-28T14:00:02+07:00", SourceUrl = "https://www.detik.com/jabar/berita/d-8508082/organ-hati-ratusan-hewan-kurban-di-bandung-terinfeksi-cacing" },
                new { Code = "NEWS-DETIK-033", Title = "Siasat Kekinian Pedagang Hewan Kurban di Bandung", Summary = "Cuaca panas di Jalan Cihampelas, Bandung, namun arus kendaraan padat. Tenda jual domba kurban menggunakan live streaming untuk menarik pembeli.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/26/penjual-domba-di-bandung-live-streaming-untuk-tingkatkan-penjualan-1779779151040_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-26T14:07:24+07:00", SourceUrl = "https://www.detik.com/jabar/bisnis/d-8505684/siasat-kekinian-pedagang-hewan-kurban-di-bandung" },
                new { Code = "NEWS-DETIK-034", Title = "Domba Merino untuk Seserahan di Ponorogo Sempat Dikira Akan Disembelih", Summary = "Domba Merino seharga Rp 5 juta jadi seserahan pernikahan unik di Ponorogo. Malindo surprise istrinya, Wilujeng, yang mengidamkan domba ini.", Image = "https://awsimages.detik.net.id/api/wm/2026/08/09/domba-merino-jadi-seserahan-pernikahan-di-ponorogo-1786267195284_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-08-09T19:00:14+07:00", SourceUrl = "https://www.detik.com/jatim/berita/d-8611039/domba-merino-untuk-seserahan-di-ponorogo-sempat-dikira-akan-disembelih" },
                new { Code = "NEWS-DETIK-035", Title = "Pesta Patok Tasikmalaya, Ajang Silaturahmi dan Dongkrak Harga Ternak", Summary = "Pesta Patok di Tasikmalaya meriahkan HUT ke-394 Kabupaten dan HUT ke-80 Bhayangkara. Kontes ternak ini tingkatkan ekonomi dan budaya peternak.", Image = "https://awsimages.detik.net.id/api/wm/2026/06/27/pesta-patok-tasikmalaya-1782538123127_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-06-27T18:00:27+07:00", SourceUrl = "https://www.detik.com/jabar/budaya/d-8549554/pesta-patok-tasikmalaya-ajang-silaturahmi-dan-dongkrak-harga-ternak" },
                new { Code = "NEWS-DETIK-036", Title = "Strategi Peternak Milenial Indramayu Jual Ratusan Domba Kurban", Summary = "Menjelang Iduladha, penjualan hewan kurban di Indramayu meningkat. Pengkuh, pemilik Bale Domba, sukses menjual 172 ekor, mendukung peternak lokal.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/26/pengkuh-triaji-hutomo-peternak-muda-asal-indramayu-1779797295881_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-26T21:15:44+07:00", SourceUrl = "https://www.detik.com/jabar/cirebon-raya/d-8506380/strategi-peternak-milenial-indramayu-jual-ratusan-domba-kurban" },
                new { Code = "NEWS-DETIK-037", Title = "Cerita Pemberdayaan Ekonomi Warga Sayana Kuningan Lewat Balai Ternak", Summary = "Sodri, peternak di Balai Ternak BAZNAS Kuningan, berbagi pengalaman beralih dari perantau ke peternak. Ia mengolah limbah ternak menjadi pupuk organik.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/27/sodri-dan-rusdianto-saat-sedang-mempersiapkan-ternak-untuk-kurban-dan-ketua-baznas-ri-sodik-mudjahid-1779882380806_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-27T20:30:00+07:00", SourceUrl = "https://www.detik.com/jabar/cirebon-raya/d-8507496/cerita-pemberdayaan-ekonomi-warga-sayana-kuningan-lewat-balai-ternak" },
                new { Code = "NEWS-DETIK-038", Title = "Uniknya 'Pilkada' di Majalengka yang Diikuti Peternak dan Domba", Summary = "Kampung Kaputren, Desa Putridalem, Kecamatan Jatitujuh, Kabupaten Majalengka, menggelar Pilkada alias Pemilihan Kandang dan Domba. Begini keseruannya.", Image = "https://awsimages.detik.net.id/api/wm/2024/06/09/pilkada-pemilihan-kandang-dan-domba-di-majalengka-6_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2024-06-09T18:10:52+07:00", SourceUrl = "https://www.detik.com/jabar/foto/d-7382350/uniknya-pilkada-di-majalengka-yang-diikuti-peternak-dan-domba" },
                new { Code = "NEWS-DETIK-039", Title = "Domba Merino Jadi Bintang Saat Libur Lebaran di Garut", Summary = "Taman Satwa Cikembulan di Garut ramai pengunjung saat libur lebaran 2026. Wisatawan dapat berinteraksi dengan domba merino dan berbagai hewan lainnya.", Image = "https://awsimages.detik.net.id/api/wm/2026/03/26/domba-merino-di-taman-satwa-cikembulan-garut-1774512633514_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-03-26T16:30:30+07:00", SourceUrl = "https://www.detik.com/jabar/wisata/d-8416514/domba-merino-jadi-bintang-saat-libur-lebaran-di-garut" },
                new { Code = "NEWS-DETIK-040", Title = "Pedagang Kambing Kota Batu Viral, Netizen Salfok Penampilan Mirip Yesus", Summary = "Viral video penjual kambing Kota Batu bernama Faiq. Netizen salfok karena penampilannya mirip dengan visualisasi Yesus.", Image = "https://awsimages.detik.net.id/api/wm/2026/07/19/pedagang-kambing-di-kota-batu-yang-viral-disebut-berpenampilan-mirip-yesus-1784464219518_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-07-19T19:45:55+07:00", SourceUrl = "https://www.detik.com/jatim/berita/d-8581013/pedagang-kambing-kota-batu-viral-netizen-salfok-penampilan-mirip-yesus" },
                new { Code = "NEWS-DETIK-041", Title = "Disnakkan Trenggalek Temukan Puluhan Kasus Cacing Hati pada Hewan Kurban", Summary = "Dinas Peternakan Trenggalek temukan cacing hati pada hewan kurban. Petugas rekomendasikan organ dalam tidak dikonsumsi, namun daging tetap layak.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/28/cacing-pita-ditemukan-di-hewan-kurban-trenggalek-1779967908339_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-28T22:30:26+07:00", SourceUrl = "https://www.detik.com/jatim/berita/d-8508718/disnakkan-trenggalek-temukan-puluhan-kasus-cacing-hati-pada-hewan-kurban" },
                new { Code = "NEWS-DETIK-042", Title = "Bediding Bikin Sektor Peternakan di Kota Batu Waswas", Summary = "Fenomena bediding di Malang Raya berdampak pada peternakan, meningkatkan risiko penyakit hewan. Dinas Pertanian minta peternak lakukan perawatan ekstra.", Image = "https://awsimages.detik.net.id/api/wm/2026/06/09/gus-mbek-memberikan-pakan-pada-ternaknya-1781016162514_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-06-10T06:45:03+07:00", SourceUrl = "https://www.detik.com/jatim/berita/d-8525483/bediding-bikin-sektor-peternakan-di-kota-batu-waswas" },
                new { Code = "NEWS-DETIK-043", Title = "Harga Kambing-Domba di Lumajang Naik Rp 500 Ribu Jelang Kurban", Summary = "Harga kambing dan domba di Lumajang naik Rp 500 ribu menjelang Idul Adha 2026. Permintaan tinggi membuat penjualan hewan kurban semakin ramai.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/22/domba-di-pasar-hewan-lumajang-1779416692104_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-22T09:25:23+07:00", SourceUrl = "https://www.detik.com/jatim/bisnis/d-8499513/harga-kambing-domba-di-lumajang-naik-rp-500-ribu-jelang-kurban" },
                new { Code = "NEWS-DETIK-044", Title = "Potret Peternak dan Perawat Kambing Kontes di Mojokerto", Summary = "Muhammad Amin (50) warga Desa Parengan, Jetis, Mojokerto ini melayani pengobatan hingga pengawinan kambing kontes. Dari pekerjaan itu bisa untung puluhan juta.", Image = "https://awsimages.detik.net.id/api/wm/2025/02/15/perawatan-kambing-kontes-di-mojokerto-3_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-02-15T19:55:43+07:00", SourceUrl = "https://www.detik.com/jatim/foto/d-7780224/potret-peternak-dan-perawat-kambing-kontes-di-mojokerto" },
                new { Code = "NEWS-DETIK-045", Title = "Peternakan Kambing di Mojokerto Mulai Ramai Pembeli Jelang Idul Adha", Summary = "Peternakan kambing dan domba di Dusun Grogol Gede, Desa Gebangmalang, Mojoanyar, Mojokerto ramai pembeli jelang Hari Raya Idul Adha. Ada garansi lho!.", Image = "https://awsimages.detik.net.id/api/wm/2025/05/10/perternakan-kambing-di-mojokerto-mulai-ramai-pembeli-jelang-idul-adha-1746853787118_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-05-10T13:19:33+07:00", SourceUrl = "https://www.detik.com/jatim/foto/d-7908548/peternakan-kambing-di-mojokerto-mulai-ramai-pembeli-jelang-idul-adha" },
                new { Code = "NEWS-DETIK-046", Title = "Menengok Salon Kambing Gratis di Jombang", Summary = "Ada cara unik peternak kambing di Jombang, untuk menarik minat pembeli. Yaitu dengan fasilitas salon kamping gratis.", Image = "https://akcdn.detik.net.id/api/wm/2025/05/21/menengok-salon-kambing-di-jombang-1747800586641_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-05-21T11:24:50+07:00", SourceUrl = "https://www.detik.com/jatim/foto/d-7924516/menengok-salon-kambing-gratis-di-jombang" },
                new { Code = "NEWS-DETIK-047", Title = "Kisah Mila Alumni UGM, Modal 5 Kambing Kini Raup Cuan Ratusan Juta", Summary = "Kisah sukses alumni Fakultas Peternakan UGM, Mila (27), mengelola usaha peternakan modern kambing dan domba hingga beromzet ratusan juta rupiah.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/05/ilustrasi-kambing-kurban-1777952225607_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-06-30T13:35:16+07:00", SourceUrl = "https://www.detik.com/jogja/kota-pelajar/d-8553648/kisah-mila-alumni-ugm-modal-5-kambing-kini-raup-cuan-ratusan-juta" },
                new { Code = "NEWS-DETIK-048", Title = "20 Domba Warga Diduga Diterkam Macan Tutul", Summary = "Sebanyak 20 ekor domba milik warga di Kabupaten Sukabumi, Jawa Barat, ditemukan mati mengenaskan. Diduga, domba itu tewas usai serangan macan tutul.", Image = "https://awsimages.detik.net.id/api/wm/2025/09/07/domba-diduga-dimangsa-macan-tutul-1757219507697_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-09-07T21:30:49+07:00", SourceUrl = "https://www.detik.com/sumbagsel/berita/d-8100240/20-domba-warga-diduga-diterkam-macan-tutul" },
                new { Code = "NEWS-DETIK-049", Title = "Pemprov Sumsel Pastikan Stok dan Kesehatan Hewan Kurban Jelang Idul Adha", Summary = "Pemerintah Provinsi Sumatera Selatan memastikan ketersediaan hewan kurban Idul Adha 2026 aman dan mencukupi, dengan pengawasan kesehatan hewan yang ketat.", Image = "https://awsimages.detik.net.id/api/wm/2026/05/08/ilustrasi-sapi-sapi-kurban-di-ud-segar-farm-pekuncen-wirobrajan-kota-jogja-jumat-852026-1778227204287_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2026-05-26T22:40:34+07:00", SourceUrl = "https://www.detik.com/sumbagsel/berita/d-8506151/pemprov-sumsel-pastikan-stok-dan-kesehatan-hewan-kurban-jelang-idul-adha" },
                new { Code = "NEWS-DETIK-050", Title = "Menteri Agus Cek Ternak Domba di Nusakambangan, Pastikan Program Pangan Berjalan", Summary = "Menteri mipas Agus Andrianto mengecek perkembangan peternakan domba di Lapas Kembang Kuning, Nusakambangan, Cilacap, Jawa Tengah (Jateng).", Image = "https://awsimages.detik.net.id/api/wm/2025/04/17/menteri-imigrasi-dan-permasyarakatan-imipas-agus-andrianto-mengecek-perkembangan-peternakan-domba-di-lapas-kembang-kuning-nusa-1744897984761_169.jpeg?wid=54&w=1200&v=1&t=jpeg", PublishedAt = "2025-04-17T20:56:54+07:00", SourceUrl = "https://news.detik.com/berita/d-7874167/menteri-agus-cek-ternak-domba-di-nusakambangan-pastikan-program-pangan-berjalan" },
            };

            var now = DateTime.Now;
            var codes = newsItems.Select(x => x.Code).ToArray();
            var existingNews = await db.News.Where(x => codes.Contains(x.Code)).ToDictionaryAsync(x => x.Code);

            foreach (var item in newsItems)
            {
                var publishedAt = DateTimeOffset.Parse(item.PublishedAt, System.Globalization.CultureInfo.InvariantCulture).LocalDateTime;
                var content = BuildDetikDerivedContent(item.Title, item.Summary, item.SourceUrl, publishedAt);

                if (!existingNews.TryGetValue(item.Code, out var news))
                {
                    news = new News
                    {
                        Id = Guid.NewGuid(),
                        Code = item.Code,
                        CreatedBy = "system",
                        DateCreated = now
                    };
                    db.News.Add(news);
                }

                news.Title = item.Title;
                news.ShortContent = item.Summary;
                news.Content = content;
                news.ImageThumbnail = $"/uploads/image/news-detik-{item.Code[^3..]}.jpg";
                news.StartDate = publishedAt;
                news.EndDate = null;
                news.ModifiedBy = "system";
                news.DateModified = now;
                news.IsActive = true;
            }

            await db.SaveChangesAsync();
        }

        private static string BuildDetikDerivedContent(string title, string summary, string sourceUrl, DateTime publishedAt)
        {
            var encodedTitle = System.Net.WebUtility.HtmlEncode(title);
            var encodedSourceUrl = System.Net.WebUtility.HtmlEncode(sourceUrl);
            var category = GetDetikCategory(sourceUrl);
            var sentences = summary
                .Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(sentence => sentence.Trim().TrimEnd('.', '!', '?'))
                .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
                .ToList();

            var detailParagraphs = string.Join(string.Empty, sentences.Select((sentence, index) =>
            {
                var encodedSentence = System.Net.WebUtility.HtmlEncode(sentence);
                return index == 0
                    ? $"<p><strong>{encodedSentence}.</strong></p>"
                    : $"<p>{encodedSentence}.</p>";
            }));
            var articleBody = BuildDummyNewsBody(title);

            return $"""
                <article class="news-detail-content">
                    <p class="news-lead">{System.Net.WebUtility.HtmlEncode(summary)}</p>
                    <h3>Pokok Berita</h3>
                    {detailParagraphs}
                    {articleBody}
                    <hr />
                    <p><strong>Judul sumber:</strong> {encodedTitle}<br />
                    <strong>Kategori:</strong> {System.Net.WebUtility.HtmlEncode(category)}<br />
                    <strong>Tanggal terbit:</strong> {publishedAt:dd MMMM yyyy HH:mm}<br />
                    <strong>Sumber:</strong> <a href="{encodedSourceUrl}" target="_blank" rel="noopener noreferrer">detikcom</a></p>
                </article>
                """;
        }

        private static string BuildDummyNewsBody(string title)
        {
            var normalized = title.ToLowerInvariant();
            string[] paragraphs;

            if (normalized.Contains("panduan memilih") || normalized.Contains("tips memilih"))
            {
                paragraphs = new[]
                {
                    "Calon pembeli perlu mengamati kondisi hewan secara menyeluruh sebelum menentukan pilihan. Hewan yang sehat umumnya aktif, memiliki nafsu makan baik, mata jernih, bulu bersih, serta tidak menunjukkan luka atau gangguan pernapasan.",
                    "Umur hewan juga harus memenuhi ketentuan kurban. Pemeriksaan dapat dilakukan melalui catatan ternak dan kondisi gigi, kemudian dilengkapi dengan pengecekan kaki, telinga, mata, serta bentuk tubuh untuk memastikan tidak ada cacat yang mengurangi kelayakannya.",
                    "Pembelian sebaiknya dilakukan dari peternak atau tempat penjualan yang memiliki pemeriksaan kesehatan. Setelah dipilih, hewan perlu memperoleh pakan, air minum, tempat berteduh, dan waktu istirahat yang cukup agar kondisinya tetap baik sampai hari penyembelihan.",
                    "Kebersihan lokasi penampungan dan penanganan yang tidak membuat hewan stres menjadi bagian penting dari persiapan. Dengan pemeriksaan yang teliti, masyarakat dapat memilih hewan yang layak sekaligus menjaga keamanan daging yang nantinya dibagikan."
                };
            }
            else if (normalized.Contains("kurban") || normalized.Contains("idul adha") || normalized.Contains("iduladha"))
            {
                paragraphs = new[]
                {
                    "Menjelang Idul Adha, aktivitas peternak, pedagang, petugas kesehatan hewan, dan panitia kurban meningkat. Persiapan dilakukan sejak pemilihan ternak, pemeriksaan kesehatan, penataan tempat penampungan, hingga pengaturan distribusi.",
                    "Kondisi fisik dan kecukupan umur menjadi perhatian utama. Petugas juga memantau asal ternak serta kebersihan lingkungan agar hewan tetap sehat selama berada di tempat penjualan maupun lokasi penyembelihan.",
                    "Masyarakat dianjurkan memilih hewan dari penjual tepercaya dan memperhatikan label atau dokumen pemeriksaan kesehatan. Langkah tersebut membantu memastikan pelaksanaan kurban berjalan tertib dan hasilnya aman untuk dikonsumsi."
                };
            }
            else if (normalized.Contains("cacing") || normalized.Contains("virus") || normalized.Contains("penyakit") || normalized.Contains("kesehatan"))
            {
                paragraphs = new[]
                {
                    "Pengawasan kesehatan ternak diperlukan untuk menemukan gangguan sejak dini dan mencegah penyebarannya. Pemeriksaan fisik, pemantauan pola makan, kebersihan kandang, serta pencatatan perubahan perilaku menjadi bagian dari langkah pencegahan.",
                    "Jika ditemukan bagian atau organ yang tidak layak, petugas harus memisahkannya sesuai prosedur. Tindakan tersebut melindungi konsumen sekaligus membantu peternak memahami perawatan yang perlu diperbaiki.",
                    "Koordinasi antara peternak, dokter hewan, pemerintah daerah, dan petugas lapangan penting agar informasi penanganan dapat diterapkan dengan cepat. Edukasi yang berkelanjutan juga membuat peternak lebih siap menghadapi risiko penyakit."
                };
            }
            else if (normalized.Contains("smart farming") || normalized.Contains("rfid") || normalized.Contains("teknologi"))
            {
                paragraphs = new[]
                {
                    "Penerapan teknologi membantu peternak mencatat identitas, kesehatan, pakan, dan aktivitas setiap ternak secara lebih teratur. Data tersebut dapat digunakan untuk melihat perubahan kondisi tanpa hanya mengandalkan pengamatan sesaat.",
                    "Sistem identifikasi digital membuat riwayat ternak lebih mudah ditelusuri. Peternak dapat mengambil keputusan berdasarkan data, mulai dari pengaturan pakan sampai penjadwalan pemeriksaan kesehatan.",
                    "Selain meningkatkan efisiensi, smart farming dapat menjadi sarana pembelajaran bagi mahasiswa dan peternak muda. Teknologi tetap perlu didampingi keterampilan pemeliharaan agar manfaatnya sesuai dengan kebutuhan di lapangan."
                };
            }
            else if (normalized.Contains("kontes") || normalized.Contains("pesta patok") || normalized.Contains("ketangkasan") || normalized.Contains("pilkada"))
            {
                paragraphs = new[]
                {
                    "Kegiatan yang mempertemukan peternak menjadi ruang untuk menampilkan kualitas ternak sekaligus bertukar pengalaman. Penilaian biasanya menarik perhatian pada kesehatan, postur, perawatan, dan karakteristik unggulan setiap hewan.",
                    "Bagi peternak, ajang semacam ini tidak hanya mengejar penghargaan. Pertemuan dengan pembeli dan sesama pelaku usaha membuka peluang promosi, memperluas jaringan, serta meningkatkan nilai jual ternak.",
                    "Kehadiran masyarakat turut menghidupkan kegiatan ekonomi di sekitar lokasi. Tradisi peternakan pun dapat dikenal lebih luas selama penyelenggara tetap mengutamakan keamanan dan kesejahteraan hewan."
                };
            }
            else if (normalized.Contains("usaha") || normalized.Contains("harga") || normalized.Contains("pedagang") || normalized.Contains("pembeli") || normalized.Contains("cuan"))
            {
                paragraphs = new[]
                {
                    "Usaha ternak membutuhkan perencanaan pakan, kesehatan, kandang, dan pemasaran yang berjalan bersamaan. Perubahan permintaan serta biaya pemeliharaan dapat langsung memengaruhi harga jual dan keuntungan peternak.",
                    "Pelaku usaha mulai memanfaatkan media sosial dan layanan digital untuk menjangkau konsumen. Informasi mengenai kondisi, bobot, harga, dan asal ternak dapat disampaikan lebih cepat sehingga pembeli memiliki lebih banyak bahan pertimbangan.",
                    "Keberlanjutan usaha tetap bergantung pada kualitas pemeliharaan dan kepercayaan pelanggan. Pencatatan biaya serta evaluasi penjualan membantu peternak menentukan strategi yang lebih tepat untuk periode berikutnya."
                };
            }
            else if (normalized.Contains("wisata") || normalized.Contains("peternakan") || normalized.Contains("farm") || normalized.Contains("kandang"))
            {
                paragraphs = new[]
                {
                    "Peternakan menjadi tempat berlangsungnya rangkaian pemeliharaan, mulai dari pemberian pakan, pembersihan kandang, pemeriksaan kesehatan, hingga pengelolaan hasil ternak. Setiap kegiatan memerlukan jadwal dan pengawasan yang konsisten.",
                    "Kandang yang bersih, sirkulasi udara yang baik, dan ketersediaan air membantu menjaga kenyamanan hewan. Peternak juga perlu mencatat pertumbuhan serta perubahan kondisi agar masalah dapat ditangani lebih awal.",
                    "Ketika dibuka sebagai sarana edukasi, pengunjung dapat mengenal proses peternakan secara langsung. Interaksi tetap harus dilakukan secara tertib supaya tidak mengganggu hewan maupun aktivitas pekerja."
                };
            }
            else if (normalized.Contains("lulus") || normalized.Contains("mahasiswa") || normalized.Contains("napi") || normalized.Contains("pemberdayaan"))
            {
                paragraphs = new[]
                {
                    "Keterampilan peternakan dapat membuka kesempatan belajar dan sumber penghasilan baru. Peserta tidak hanya berlatih memberi pakan, tetapi juga memahami kebersihan kandang, kesehatan ternak, dan pencatatan usaha.",
                    "Proses tersebut memerlukan ketekunan karena hasil pemeliharaan tidak diperoleh secara instan. Pendampingan dari tenaga berpengalaman membantu peserta menerapkan praktik yang benar dan membangun rasa percaya diri.",
                    "Pengalaman mengelola ternak dapat dikembangkan menjadi usaha setelah pelatihan selesai. Dengan akses pasar dan pengelolaan yang baik, kegiatan peternakan berpotensi mendukung kemandirian ekonomi."
                };
            }
            else if (normalized.Contains("terbakar") || normalized.Contains("maling") || normalized.Contains("diterkam"))
            {
                paragraphs = new[]
                {
                    "Peristiwa yang menimpa ternak menimbulkan kerugian bagi pemilik dan menjadi pengingat pentingnya keamanan kandang. Pemeriksaan pagar, akses keluar-masuk, penerangan, serta kondisi instalasi perlu dilakukan secara berkala.",
                    "Peternak dapat menyiapkan pencatatan jumlah ternak dan prosedur darurat agar penanganan berlangsung lebih cepat. Komunikasi dengan warga sekitar juga membantu pengawasan, terutama pada malam hari.",
                    "Setelah kejadian, pendataan kerusakan dan evaluasi penyebab diperlukan sebelum kandang kembali digunakan. Langkah pencegahan yang diperbaiki dapat mengurangi risiko kejadian serupa."
                };
            }
            else
            {
                paragraphs = new[]
                {
                    "Perkembangan sektor peternakan berkaitan erat dengan cara pemeliharaan, kesehatan hewan, ketersediaan pakan, dan akses pasar. Keempat unsur tersebut menentukan kualitas ternak serta keberlanjutan usaha.",
                    "Peternak perlu melakukan pengamatan rutin dan mencatat setiap perubahan kondisi. Data yang tersusun membantu menentukan tindakan perawatan sekaligus menjadi bahan evaluasi produksi.",
                    "Dukungan pemerintah, tenaga kesehatan hewan, komunitas, dan konsumen dapat memperkuat ekosistem peternakan. Kolaborasi membuat informasi dan praktik pemeliharaan yang baik lebih mudah diterapkan."
                };
            }

            return string.Join(string.Empty, paragraphs.Select(paragraph => $"<p>{System.Net.WebUtility.HtmlEncode(paragraph)}</p>"));
        }

        private static string GetDetikCategory(string sourceUrl)
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            {
                return "Peternakan";
            }

            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath.ToLowerInvariant();
            if (host.StartsWith("finance.")) return "Bisnis";
            if (host.StartsWith("food.")) return "Kuliner";
            if (host.StartsWith("travel.")) return "Wisata";
            if (path.Contains("/edu/")) return "Edukasi";
            if (path.Contains("/hikmah/")) return "Khazanah";
            if (path.Contains("/bisnis/")) return "Bisnis";
            if (path.Contains("/budaya/")) return "Budaya";
            if (path.Contains("/wisata/")) return "Wisata";
            if (path.Contains("/foto/")) return "Foto";
            return "Berita";
        }

        private static async Task AddIndonesiaDashboardSeedAsync(ApplicationDbContext db, ReminderTemplate template)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var provinces = new[]
            {
                new { Code = "AC", Name = "Aceh", Latitude = 4.6951, Longitude = 96.7494 },
                new { Code = "SU", Name = "Sumatera Utara", Latitude = 2.1154, Longitude = 99.5451 },
                new { Code = "SB", Name = "Sumatera Barat", Latitude = -0.7399, Longitude = 100.8000 },
                new { Code = "RI", Name = "Riau", Latitude = 0.2933, Longitude = 101.7068 },
                new { Code = "KR", Name = "Kepulauan Riau", Latitude = 3.9457, Longitude = 108.1429 },
                new { Code = "JA", Name = "Jambi", Latitude = -1.6101, Longitude = 103.6131 },
                new { Code = "SS", Name = "Sumatera Selatan", Latitude = -3.3194, Longitude = 103.9144 },
                new { Code = "BB", Name = "Kepulauan Bangka Belitung", Latitude = -2.7411, Longitude = 106.4406 },
                new { Code = "BE", Name = "Bengkulu", Latitude = -3.5778, Longitude = 102.3464 },
                new { Code = "LA", Name = "Lampung", Latitude = -4.5586, Longitude = 105.4068 },
                new { Code = "JK", Name = "DKI Jakarta", Latitude = -6.2088, Longitude = 106.8456 },
                new { Code = "BT", Name = "Banten", Latitude = -6.4058, Longitude = 106.0640 },
                new { Code = "JB", Name = "Jawa Barat", Latitude = -6.9175, Longitude = 107.6191 },
                new { Code = "JT", Name = "Jawa Tengah", Latitude = -7.1510, Longitude = 110.1403 },
                new { Code = "YO", Name = "DI Yogyakarta", Latitude = -7.7956, Longitude = 110.3695 },
                new { Code = "JI", Name = "Jawa Timur", Latitude = -7.5361, Longitude = 112.2384 },
                new { Code = "BA", Name = "Bali", Latitude = -8.4095, Longitude = 115.1889 },
                new { Code = "NB", Name = "Nusa Tenggara Barat", Latitude = -8.6529, Longitude = 117.3616 },
                new { Code = "NT", Name = "Nusa Tenggara Timur", Latitude = -8.6574, Longitude = 121.0794 },
                new { Code = "KB", Name = "Kalimantan Barat", Latitude = -0.2788, Longitude = 111.4753 },
                new { Code = "KT", Name = "Kalimantan Tengah", Latitude = -1.6815, Longitude = 113.3824 },
                new { Code = "KS", Name = "Kalimantan Selatan", Latitude = -3.0926, Longitude = 115.2838 },
                new { Code = "KI", Name = "Kalimantan Timur", Latitude = 0.5387, Longitude = 116.4194 },
                new { Code = "KU", Name = "Kalimantan Utara", Latitude = 3.0731, Longitude = 116.0414 },
                new { Code = "SA", Name = "Sulawesi Utara", Latitude = 0.6247, Longitude = 123.9750 },
                new { Code = "GO", Name = "Gorontalo", Latitude = 0.6999, Longitude = 122.4467 },
                new { Code = "ST", Name = "Sulawesi Tengah", Latitude = -1.4300, Longitude = 121.4456 },
                new { Code = "SR", Name = "Sulawesi Barat", Latitude = -2.8441, Longitude = 119.2321 },
                new { Code = "SN", Name = "Sulawesi Selatan", Latitude = -3.6688, Longitude = 119.9741 },
                new { Code = "SG", Name = "Sulawesi Tenggara", Latitude = -4.1449, Longitude = 122.1746 },
                new { Code = "MA", Name = "Maluku", Latitude = -3.2385, Longitude = 130.1453 },
                new { Code = "MU", Name = "Maluku Utara", Latitude = 1.5709, Longitude = 127.8088 },
                new { Code = "PA", Name = "Papua", Latitude = -4.2699, Longitude = 138.0804 },
                new { Code = "PB", Name = "Papua Barat", Latitude = -1.3361, Longitude = 133.1747 },
                new { Code = "PD", Name = "Papua Barat Daya", Latitude = -1.0395, Longitude = 131.8523 },
                new { Code = "PT", Name = "Papua Tengah", Latitude = -3.5403, Longitude = 136.5656 },
                new { Code = "PG", Name = "Papua Pegunungan", Latitude = -4.0836, Longitude = 138.9437 },
                new { Code = "PS", Name = "Papua Selatan", Latitude = -7.6980, Longitude = 139.6510 }
            };

            for (var provinceIndex = 0; provinceIndex < provinces.Length; provinceIndex++)
            {
                var province = provinces[provinceIndex];
                var farmCode = $"FRM-{province.Code}-PROV";
                var farm = await db.Farm.FirstOrDefaultAsync(x => x.Code == farmCode);
                if (farm == null)
                {
                    farm = new Farm
                    {
                        Id = Guid.NewGuid(),
                        Code = farmCode,
                        Name = $"SmartSheep {province.Name}",
                        Location = province.Name,
                        Address = $"Provinsi {province.Name}, Indonesia",
                        Latitude = province.Latitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture),
                        Longitude = province.Longitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture),
                        CreatedBy = "system",
                        ModifiedBy = "system",
                        DateCreated = now,
                        DateModified = now,
                        IsActive = true
                    };
                    db.Farm.Add(farm);
                }
                else
                {
                    farm.Name = $"SmartSheep {province.Name}";
                    farm.Location = province.Name;
                    farm.Address = $"Provinsi {province.Name}, Indonesia";
                    farm.Latitude = province.Latitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                    farm.Longitude = province.Longitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                    farm.ModifiedBy = "system";
                    farm.DateModified = now;
                    farm.IsActive = true;
                }

                var barnCount = provinceIndex % 5 == 0 ? 2 : 1;
                for (var barnIndex = 1; barnIndex <= barnCount; barnIndex++)
                {
                    var livestockCode = $"SHP-{province.Code}-{barnIndex:00}";
                    var livestock = await db.Livestock.FirstOrDefaultAsync(x => x.Code == livestockCode);
                    if (livestock == null)
                    {
                        livestock = new Livestock
                        {
                            Id = Guid.NewGuid(),
                            Code = livestockCode,
                            Name = $"Domba {province.Name} {barnIndex}",
                            Sex = (provinceIndex + barnIndex) % 2 == 0 ? "Male" : "Female",
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };
                        db.Livestock.Add(livestock);
                    }

                    var barnCode = $"BRN-{province.Code}-{barnIndex:00}";
                    var barn = await db.Barn.FirstOrDefaultAsync(x => x.Code == barnCode);
                    if (barn == null)
                    {
                        barn = new Barn
                        {
                            Id = Guid.NewGuid(),
                            Code = barnCode,
                            Name = $"Kandang {province.Name} {barnIndex}",
                            Capacity = 20 + ((provinceIndex + barnIndex) % 4) * 10,
                            Length = 10 + ((provinceIndex + barnIndex) % 5) * 2.5,
                            Width = 6 + ((provinceIndex + barnIndex) % 4) * 1.5,
                            Type = (provinceIndex + barnIndex) % 3 == 0 ? "Large" : "Small",
                            FkFarmId = farm.Id,
                            FkLivestockId = livestock.Id,
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };
                        db.Barn.Add(barn);
                    }

                    var sourceCode = $"SRC-{province.Code}-{barnIndex:00}-01";
                    var sourceVideo = await db.SourceVideo.FirstOrDefaultAsync(x => x.Code == sourceCode);
                    if (sourceVideo == null)
                    {
                        sourceVideo = new SourceVideo
                        {
                            Id = Guid.NewGuid(),
                            FkBarnId = barn.Id,
                            Code = sourceCode,
                            SourceVideoUrl = "https://www.youtube.com/watch?v=pBFmbzw0cdw",
                            IsOnline = (provinceIndex + barnIndex) % 4 != 0,
                            CreatedBy = "system",
                            ModifiedBy = "system",
                            DateCreated = now,
                            DateModified = now,
                            IsActive = true
                        };
                        db.SourceVideo.Add(sourceVideo);
                    }

                    var hasDashboardActivities = await db.SucklingActivity.AnyAsync(x =>
                        x.FkBarnId == barn.Id && x.ActivityStart >= today.AddDays(-13));
                    if (hasDashboardActivities)
                    {
                        continue;
                    }

                    for (var dayOffset = 0; dayOffset < 14; dayOffset++)
                    {
                        var day = today.AddDays(-dayOffset);
                        var dayFrequency = 0;
                        var dayDurationSeconds = 0;

                        for (var hour = 5; hour <= 21; hour += 2)
                        {
                            var variation = (provinceIndex * 31 + barnIndex * 17 + dayOffset * 13 + hour * 7) % 95;
                            var peakBonus = hour <= 9 || hour >= 17 ? 45 : 10;
                            var frequency = 25 + variation + peakBonus;
                            var durationMinutes = 8 + ((variation * 3 + hour) % 65) + (peakBonus / 3);
                            var activityStart = day.AddHours(hour).AddMinutes((provinceIndex * 7 + dayOffset * 3) % 40);

                            db.SucklingActivity.Add(new SucklingActivity
                            {
                                Id = Guid.NewGuid(),
                                FkBarnId = barn.Id,
                                ActivityStart = activityStart,
                                ActivityEnd = activityStart.AddMinutes(durationMinutes),
                                TotalFrequency = frequency,
                                TotalDurationSeconds = durationMinutes * 60,
                                CreatedBy = "system",
                                ModifiedBy = "system",
                                DateCreated = now,
                                DateModified = now,
                                IsActive = true
                            });
                            dayFrequency += frequency;
                            dayDurationSeconds += durationMinutes * 60;
                        }

                        var statisticCode = $"STAT-{province.Code}-{barnIndex:00}-{day:yyyyMMdd}";
                        var statistic = await db.SucklingStatistic.FirstOrDefaultAsync(x => x.Code == statisticCode);
                        if (statistic == null)
                        {
                            var requiresReminder = (provinceIndex + barnIndex + dayOffset) % 6 == 0;
                            statistic = new SucklingStatistic
                            {
                                Id = Guid.NewGuid(),
                                Code = statisticCode,
                                FkBarnId = barn.Id,
                                PeriodDescription = $"Aktivitas harian {province.Name}",
                                TotalFrequency = dayFrequency,
                                TotalDurationSeconds = dayDurationSeconds,
                                RequiresReminder = requiresReminder,
                                ActivityFrom = day.AddHours(5),
                                ActivityTo = day.AddHours(22),
                                HourNumber = 18,
                                CreatedBy = "system",
                                ModifiedBy = "system",
                                DateCreated = now,
                                DateModified = now,
                                IsActive = true
                            };
                            db.SucklingStatistic.Add(statistic);

                            if (requiresReminder)
                            {
                                var reminderStatus = ((provinceIndex + dayOffset) % 3) switch
                                {
                                    0 => "Pending",
                                    1 => "Sent",
                                    _ => "Failed"
                                };
                                db.ReminderLog.Add(new ReminderLog
                                {
                                    Id = Guid.NewGuid(),
                                    FkReminderTemplateId = template.Id,
                                    FkSucklingStatisticId = statistic.Id,
                                    Username = "SuperAdmin",
                                    Status = reminderStatus,
                                    ScheduledAt = day.AddHours(20),
                                    SentAt = reminderStatus == "Sent" ? day.AddHours(20).AddMinutes(5 + provinceIndex % 20) : null,
                                    ErrorMessage = reminderStatus == "Failed" ? "Simulated notification delivery failure." : null,
                                    CreatedBy = "system",
                                    ModifiedBy = "system",
                                    DateCreated = now,
                                    DateModified = now,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                if (provinceIndex % 6 == 5)
                {
                    await db.SaveChangesAsync();
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
