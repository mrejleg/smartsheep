using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeFarmData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FkFarmId",
                table: "SystemNotification",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FkBarnId",
                table: "SucklingStatistic",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotification_FkFarmId",
                table: "SystemNotification",
                column: "FkFarmId");

            migrationBuilder.CreateIndex(
                name: "IX_SucklingStatistic_FkBarnId",
                table: "SucklingStatistic",
                column: "FkBarnId");

            migrationBuilder.AddForeignKey(
                name: "FK_SucklingStatistic_Barn_FkBarnId",
                table: "SucklingStatistic",
                column: "FkBarnId",
                principalTable: "Barn",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                -- Backfill only exact, unambiguous historical seed formats.
                -- Unknown records stay unassigned (never visible across farms).
                ;WITH Matches AS (
                    SELECT s.Id, b.Id AS BarnId, COUNT(*) OVER (PARTITION BY s.Id) AS Matches
                    FROM dbo.SucklingStatistic s
                    JOIN dbo.Barn b ON b.Code LIKE 'BRN-%'
                    JOIN dbo.Farm f ON f.Id=b.FkFarmId
                    WHERE s.FkBarnId IS NULL AND (
                        s.Code='STAT-'+SUBSTRING(b.Code,5,50)+'-'+CONVERT(varchar(8),s.ActivityFrom,112)
                        OR s.Code='STAT-'+f.Code+'-'+CONVERT(varchar(8),s.ActivityFrom,112)+'-'+RIGHT(b.Code,2)
                    )
                )
                UPDATE s SET FkBarnId=m.BarnId FROM dbo.SucklingStatistic s JOIN Matches m ON m.Id=s.Id WHERE m.Matches=1;

                UPDATE n SET FkFarmId=b.FkFarmId FROM dbo.SystemNotification n
                JOIN dbo.ReminderLog r ON n.Code='ReminderLog:'+CONVERT(varchar(36),r.Id)
                JOIN dbo.SucklingStatistic s ON s.Id=r.FkSucklingStatisticId
                JOIN dbo.Barn b ON b.Id=s.FkBarnId WHERE n.FkFarmId IS NULL;
                UPDATE n SET FkFarmId=b.FkFarmId FROM dbo.SystemNotification n
                JOIN dbo.SourceVideoStatusLog l ON n.Code='SourceVideoStatusLog:'+CONVERT(varchar(36),l.Id)
                JOIN dbo.SourceVideo v ON v.Id=l.FkIdSourceVideo
                JOIN dbo.Barn b ON b.Id=v.FkBarnId WHERE n.FkFarmId IS NULL;

                -- Explicit rollout requested by the owner, separate from startup
                -- bootstrap: add missing SuperAdmin links without changing others.
                INSERT dbo.UserFarm (Id,FkUserId,FkFarmId,IsActive,CreatedBy,ModifiedBy,DateCreated,DateModified)
                SELECT NEWID(),u.Id,f.Id,1,'farm-scope-rollout','farm-scope-rollout',GETDATE(),GETDATE()
                FROM dbo.UserLogin u CROSS JOIN dbo.Farm f
                WHERE u.Username='SuperAdmin' AND u.IsActive=1
                    AND NOT EXISTS (SELECT 1 FROM dbo.UserFarm a WHERE a.FkUserId=u.Id AND a.FkFarmId=f.Id);
                UPDATE a SET IsActive=1,ModifiedBy='farm-scope-rollout',DateModified=GETDATE()
                FROM dbo.UserFarm a JOIN dbo.UserLogin u ON u.Id=a.FkUserId
                WHERE u.Username='SuperAdmin' AND u.IsActive=1 AND a.IsActive=0;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemNotification_Farm_FkFarmId",
                table: "SystemNotification",
                column: "FkFarmId",
                principalTable: "Farm",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SucklingStatistic_Barn_FkBarnId",
                table: "SucklingStatistic");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemNotification_Farm_FkFarmId",
                table: "SystemNotification");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotification_FkFarmId",
                table: "SystemNotification");

            migrationBuilder.DropIndex(
                name: "IX_SucklingStatistic_FkBarnId",
                table: "SucklingStatistic");

            migrationBuilder.DropColumn(
                name: "FkFarmId",
                table: "SystemNotification");

            migrationBuilder.DropColumn(
                name: "FkBarnId",
                table: "SucklingStatistic");
        }
    }
}
