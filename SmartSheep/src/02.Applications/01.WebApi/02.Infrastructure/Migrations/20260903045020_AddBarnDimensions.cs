using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBarnDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Length",
                table: "Barn",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Width",
                table: "Barn",
                type: "float",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Barn] SET [Length] = CASE WHEN UPPER([Type]) = 'LARGE' THEN 18.0 ELSE 12.0 END, " +
                "[Width] = CASE WHEN UPPER([Type]) = 'LARGE' THEN 10.0 ELSE 7.0 END " +
                "WHERE [Length] IS NULL OR [Width] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Length",
                table: "Barn");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Barn");
        }
    }
}
