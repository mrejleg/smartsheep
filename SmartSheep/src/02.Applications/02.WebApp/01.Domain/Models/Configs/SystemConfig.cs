using Project.Base.Models.Entities;

namespace Web.Domain.Models.Configs
{
    public class SystemConfig : BaseEntity<Guid>
    {
        public int StatisticsDurationMinutes { get; set; } = 120;

        public int SourceVideoHealthCheckIntervalMinutes { get; set; } = 5;

        public double YoloConfidenceThreshold { get; set; } = 0.35;

        public double YoloMinimumSucklingSeconds { get; set; } = 3;

        public double YoloSyncIdleSeconds { get; set; } = 30;

        public string YoloPendingFolder { get; set; } = "pending";

        public double YoloMaximumProcessFps { get; set; } = 10;

        public int YoloInferenceThreads { get; set; } = 2;

        public double YoloSourceRefreshSeconds { get; set; } = 60;

        public string? YoloSourceCodes { get; set; }

        public double YoloPostureTolerance { get; set; } = 0.25;

        public double YoloShrinkThreshold { get; set; } = 0.10;

        public double YoloMissingToleranceSeconds { get; set; } = 0.3;

        public double YoloUdderZoneMargin { get; set; } = 0.5;

        public double YoloEweDirectionWindowSeconds { get; set; } = 1.0;

        public double YoloEweMinimumMove { get; set; } = 0.15;

        public double YoloEweHeadMemorySeconds { get; set; } = 5;

        public double YoloUdderZoneLength { get; set; } = 0.5;

        public double YoloLambMovingWindowSeconds { get; set; } = 0.5;

        public double YoloLambMinimumSpeed { get; set; } = 0.25;

        public double YoloBaselineWeight { get; set; } = 0.2;

        public double YoloBaselineResetSeconds { get; set; } = 5;

        public double YoloSizeSmoothingSeconds { get; set; } = 1;

        public double YoloUdderZoneHysteresis { get; set; } = 0.1;

        public double YoloThresholdHysteresis { get; set; } = 0.5;

        public int YoloTrackConfirmFrames { get; set; } = 3;

        public double YoloNmsIouThreshold { get; set; } = 0.7;

        public double YoloNmsContainmentThreshold { get; set; } = 0.8;

        public double YoloLambMinimumAreaRatio { get; set; } = 0.05;

        public double YoloLambMaximumAreaRatio { get; set; } = 0.9;

        public double YoloLambPartAreaRatio { get; set; } = 0.2;

        public double YoloLambPartGap { get; set; } = 0.02;

        public int YoloEweHoldFrames { get; set; } = 10;
    }
}
