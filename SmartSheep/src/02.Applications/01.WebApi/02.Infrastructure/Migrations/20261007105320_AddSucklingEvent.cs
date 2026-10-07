using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSucklingEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LongestGapSeconds",
                table: "SucklingStatistic",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ObservationSeconds",
                table: "SucklingStatistic",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TimeCategory",
                table: "SucklingStatistic",
                type: "VARCHAR(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalApproach",
                table: "SucklingStatistic",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalFailedAttempt",
                table: "SucklingStatistic",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SucklingEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FkSucklingActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkBarnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FkSourceVideoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "VARCHAR(20)", maxLength: 20, nullable: false),
                    LambTrackId = table.Column<int>(type: "int", nullable: false),
                    EweTrackId = table.Column<int>(type: "int", nullable: false),
                    EventStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<double>(type: "float", nullable: false),
                    FrameCount = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SucklingEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SucklingEvent_Barn_FkBarnId",
                        column: x => x.FkBarnId,
                        principalTable: "Barn",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SucklingEvent_SourceVideo_FkSourceVideoId",
                        column: x => x.FkSourceVideoId,
                        principalTable: "SourceVideo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SucklingEvent_SucklingActivity_FkSucklingActivityId",
                        column: x => x.FkSucklingActivityId,
                        principalTable: "SucklingActivity",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SucklingEvent_FkBarnId",
                table: "SucklingEvent",
                column: "FkBarnId");

            migrationBuilder.CreateIndex(
                name: "IX_SucklingEvent_FkSourceVideoId",
                table: "SucklingEvent",
                column: "FkSourceVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SucklingEvent_FkSucklingActivityId",
                table: "SucklingEvent",
                column: "FkSucklingActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SucklingEvent");

            migrationBuilder.DropColumn(
                name: "LongestGapSeconds",
                table: "SucklingStatistic");

            migrationBuilder.DropColumn(
                name: "ObservationSeconds",
                table: "SucklingStatistic");

            migrationBuilder.DropColumn(
                name: "TimeCategory",
                table: "SucklingStatistic");

            migrationBuilder.DropColumn(
                name: "TotalApproach",
                table: "SucklingStatistic");

            migrationBuilder.DropColumn(
                name: "TotalFailedAttempt",
                table: "SucklingStatistic");
        }
    }
}
