namespace InferenceYolo.Models
{
    // Dibaca dari 04.Api/appsettings.json (section "Yolo"), nama property sama dengan Api.Domain.Models.AppSetting.
    public class AppSetting
    {
        public string YoloConfidenceThreshold { get; set; }
        public string YoloMinimumSucklingSeconds { get; set; }
        public string YoloSyncIdleSeconds { get; set; }
        public string YoloPendingFolder { get; set; }
        public string YoloMaximumProcessFps { get; set; }
        public string YoloInferenceThreads { get; set; }
        public string YoloSourceRefreshSeconds { get; set; }
        public string YoloSourceCodes { get; set; }
        public string YoloPostureTolerance { get; set; }
        public string YoloShrinkThreshold { get; set; }
        public string YoloMissingToleranceSeconds { get; set; }
        public string YoloUdderZoneMargin { get; set; }
        public string YoloEweDirectionWindowSeconds { get; set; }
        public string YoloEweMinimumMove { get; set; }
        public string YoloLambMovingWindowSeconds { get; set; }
        public string YoloLambMinimumSpeed { get; set; }
        public string YoloBaselineWeight { get; set; }
        public string YoloBaselineResetSeconds { get; set; }
        public string YoloUdderZoneHysteresis { get; set; }
        public string YoloThresholdHysteresis { get; set; }
        public string YoloTrackConfirmFrames { get; set; }
        public string YoloNmsIouThreshold { get; set; }
        public string YoloNmsContainmentThreshold { get; set; }
        public string YoloApiUrl { get; set; }
        public string YoloClientId { get; set; }
        public string YoloClientSecret { get; set; }
    }
}
