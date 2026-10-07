using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFarm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFarm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkUserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 20, nullable: false),
                    FkFarmId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 20, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFarm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFarm_Farm_FkFarmId",
                        column: x => x.FkFarmId,
                        principalTable: "Farm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFarm_UserLogin_FkUserId",
                        column: x => x.FkUserId,
                        principalTable: "UserLogin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFarm_FkFarmId",
                table: "UserFarm",
                column: "FkFarmId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFarm_FkUserId_FkFarmId",
                table: "UserFarm",
                columns: new[] { "FkUserId", "FkFarmId" },
                unique: true);

            // One-time additive feature registration for an existing database.
            // Startup seeding still skips populated menu/config tables.
            migrationBuilder.Sql("""
                DECLARE @menu uniqueidentifier;
                DECLARE @parent uniqueidentifier = (SELECT TOP(1) Id FROM AppMenu WHERE Name = 'Configs' ORDER BY DateCreated, Id);
                IF @parent IS NOT NULL AND NOT EXISTS (SELECT 1 FROM AppMenu WHERE Controller = 'UserFarm')
                BEGIN
                    SET @menu = NEWID();
                    INSERT INTO AppMenu (Id, FkParentId, Name, Controller, SequenceNumber, AccessTypes, IsSection, IsMobile, IsActive, Icon, IconActive, DateCreated, DateModified, CreatedBy, ModifiedBy)
                    VALUES (@menu, @parent, 'User Farms', 'UserFarm', 'c.2.5.1', 'view,add,edit,delete,detail', 0, 0, 1,
                        '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
                        '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
                        GETDATE(), GETDATE(), 'user-farm-feature', 'user-farm-feature');
                    INSERT INTO AppMenuRole (Id, FkAppMenuId, FkAppRoleId, AccessTypes, IsActive, DateCreated, DateModified, CreatedBy, ModifiedBy)
                    SELECT NEWID(), @menu, r.FkAppRoleId, r.AccessTypes, r.IsActive, GETDATE(), GETDATE(), 'user-farm-feature', 'user-farm-feature'
                    FROM AppMenuRole r
                    WHERE r.FkAppMenuId = (SELECT TOP(1) Id FROM AppMenu WHERE Controller = 'UserLogin' ORDER BY DateCreated, Id);
                END;

                INSERT INTO ScopeClient (Id, Name, IsActive, DateCreated, DateModified, CreatedBy, ModifiedBy)
                SELECT NEWID(), 'UserFarm.' + a.Name, 1, GETDATE(), GETDATE(), 'user-farm-feature', 'user-farm-feature'
                FROM (VALUES ('View'), ('Add'), ('Edit'), ('Delete')) a(Name)
                WHERE NOT EXISTS (SELECT 1 FROM ScopeClient WHERE Name = 'UserFarm.' + a.Name);

                INSERT INTO AppScopeClient (Id, FkAppClientId, FkScopeClientId, IsActive, DateCreated, DateModified, CreatedBy, ModifiedBy)
                SELECT NEWID(), source.FkAppClientId, target.Id, 1, GETDATE(), GETDATE(), 'user-farm-feature', 'user-farm-feature'
                FROM (SELECT DISTINCT m.FkAppClientId, s.Name FROM AppScopeClient m JOIN ScopeClient s ON s.Id = m.FkScopeClientId
                    WHERE m.IsActive = 1 AND s.IsActive = 1 AND s.Name IN ('AppMenu.View', 'AppMenu.Add', 'AppMenu.Edit', 'AppMenu.Delete')) source
                JOIN ScopeClient target ON target.Name = REPLACE(source.Name, 'AppMenu.', 'UserFarm.')
                WHERE target.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM AppScopeClient existing
                    WHERE existing.FkAppClientId = source.FkAppClientId AND existing.FkScopeClientId = target.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove only feature-owned menu/scope rows. Leave pre-existing configuration alone.
            migrationBuilder.Sql("""
                DELETE FROM AppScopeClient WHERE CreatedBy = 'user-farm-feature' AND FkScopeClientId IN
                    (SELECT Id FROM ScopeClient WHERE Name IN ('UserFarm.View', 'UserFarm.Add', 'UserFarm.Edit', 'UserFarm.Delete'));
                DELETE FROM ScopeClient WHERE CreatedBy = 'user-farm-feature' AND Name IN ('UserFarm.View', 'UserFarm.Add', 'UserFarm.Edit', 'UserFarm.Delete')
                    AND NOT EXISTS (SELECT 1 FROM AppScopeClient WHERE FkScopeClientId = ScopeClient.Id);
                DELETE FROM AppMenuRole WHERE CreatedBy = 'user-farm-feature' AND FkAppMenuId IN (SELECT Id FROM AppMenu WHERE Controller = 'UserFarm' AND CreatedBy = 'user-farm-feature');
                DELETE FROM AppMenu WHERE Controller = 'UserFarm' AND CreatedBy = 'user-farm-feature'
                    AND NOT EXISTS (SELECT 1 FROM AppMenuRole WHERE FkAppMenuId = AppMenu.Id)
                    AND NOT EXISTS (SELECT 1 FROM MenuFavorite WHERE FkAppMenuId = AppMenu.Id)
                    AND NOT EXISTS (SELECT 1 FROM AppMenu child WHERE child.FkParentId = AppMenu.Id);
                """);
            migrationBuilder.DropTable(
                name: "UserFarm");
        }
    }
}
