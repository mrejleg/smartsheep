using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FillSystemConfigYoloDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddSystemConfigYolo menambah kolom Yolo dengan nilai 0/'' pada SystemConfig yang sudah ada; baris yang belum
            // pernah diisi (YoloPendingFolder kosong) diisi nilai default entity SystemConfig
            migrationBuilder.Sql("""
                UPDATE [SystemConfig] SET
                    [YoloConfidenceThreshold] = 0.35,
                    [YoloMinimumSucklingSeconds] = 3,
                    [YoloSyncIdleSeconds] = 30,
                    [YoloPendingFolder] = N'pending',
                    [YoloMaximumProcessFps] = 10,
                    [YoloInferenceThreads] = 2,
                    [YoloSourceRefreshSeconds] = 60,
                    [YoloPostureTolerance] = 0.25,
                    [YoloShrinkThreshold] = 0.10,
                    [YoloMissingToleranceSeconds] = 0.3,
                    [YoloUdderZoneMargin] = 0.5,
                    [YoloEweDirectionWindowSeconds] = 1.0,
                    [YoloEweMinimumMove] = 0.15,
                    [YoloEweHeadMemorySeconds] = 5,
                    [YoloUdderZoneLength] = 0.5,
                    [YoloLambMovingWindowSeconds] = 0.5,
                    [YoloLambMinimumSpeed] = 0.25,
                    [YoloBaselineWeight] = 0.2,
                    [YoloBaselineResetSeconds] = 5,
                    [YoloSizeSmoothingSeconds] = 1,
                    [YoloUdderZoneHysteresis] = 0.1,
                    [YoloThresholdHysteresis] = 0.5,
                    [YoloTrackConfirmFrames] = 3,
                    [YoloNmsIouThreshold] = 0.7,
                    [YoloNmsContainmentThreshold] = 0.8,
                    [YoloLambMinimumAreaRatio] = 0.05,
                    [YoloLambMaximumAreaRatio] = 0.9,
                    [YoloLambPartAreaRatio] = 0.2,
                    [YoloLambPartGap] = 0.02,
                    [YoloEweHoldFrames] = 10
                WHERE [YoloPendingFolder] = N''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
