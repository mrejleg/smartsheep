using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConfigYolo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "YoloBaselineResetSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloBaselineWeight",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloConfidenceThreshold",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloEweDirectionWindowSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloEweHeadMemorySeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "YoloEweHoldFrames",
                table: "SystemConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "YoloEweMinimumMove",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "YoloInferenceThreads",
                table: "SystemConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambMaximumAreaRatio",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambMinimumAreaRatio",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambMinimumSpeed",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambMovingWindowSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambPartAreaRatio",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloLambPartGap",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloMaximumProcessFps",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloMinimumSucklingSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloMissingToleranceSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloNmsContainmentThreshold",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloNmsIouThreshold",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "YoloPendingFolder",
                table: "SystemConfig",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "YoloPostureTolerance",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloShrinkThreshold",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloSizeSmoothingSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "YoloSourceCodes",
                table: "SystemConfig",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "YoloSourceRefreshSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloSyncIdleSeconds",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloThresholdHysteresis",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "YoloTrackConfirmFrames",
                table: "SystemConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "YoloUdderZoneHysteresis",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloUdderZoneLength",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "YoloUdderZoneMargin",
                table: "SystemConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YoloBaselineResetSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloBaselineWeight",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloConfidenceThreshold",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloEweDirectionWindowSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloEweHeadMemorySeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloEweHoldFrames",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloEweMinimumMove",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloInferenceThreads",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambMaximumAreaRatio",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambMinimumAreaRatio",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambMinimumSpeed",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambMovingWindowSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambPartAreaRatio",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloLambPartGap",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloMaximumProcessFps",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloMinimumSucklingSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloMissingToleranceSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloNmsContainmentThreshold",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloNmsIouThreshold",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloPendingFolder",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloPostureTolerance",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloShrinkThreshold",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloSizeSmoothingSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloSourceCodes",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloSourceRefreshSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloSyncIdleSeconds",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloThresholdHysteresis",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloTrackConfirmFrames",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloUdderZoneHysteresis",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloUdderZoneLength",
                table: "SystemConfig");

            migrationBuilder.DropColumn(
                name: "YoloUdderZoneMargin",
                table: "SystemConfig");
        }
    }
}
