using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveStatisticReminderMinutesToBarn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatisticReminderMinutes",
                table: "Barn",
                type: "int",
                nullable: false,
                defaultValue: 120);

            // Kandang yang sudah ada memakai periode statistik SystemConfig aktif terbaru sebelum kolomnya dihapus
            migrationBuilder.Sql("""
                UPDATE [Barn] SET [StatisticReminderMinutes] = [c].[StatisticsDurationMinutes]
                FROM [Barn] CROSS JOIN (
                    SELECT TOP(1) [StatisticsDurationMinutes] FROM [SystemConfig]
                    WHERE [IsActive] = 1 AND [StatisticsDurationMinutes] > 0
                    ORDER BY COALESCE([DateModified], [DateCreated]) DESC) AS [c]
                """);

            migrationBuilder.DropColumn(
                name: "StatisticsDurationMinutes",
                table: "SystemConfig");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatisticsDurationMinutes",
                table: "SystemConfig",
                type: "int",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.DropColumn(
                name: "StatisticReminderMinutes",
                table: "Barn");
        }
    }
}
