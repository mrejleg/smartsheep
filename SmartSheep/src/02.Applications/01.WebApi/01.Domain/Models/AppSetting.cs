namespace Api.Domain.Models
{
    public class AppSetting
    {
        public string DatabaseProvider { get; set; }
        public string DatabaseNamingConvention { get; set; }

        public string ConnectionStringsDefaultConnection { get; set; }

        public string ApplicationUrlPublish { get; set; }
        public string ApplicationApiBaseUrl { get; set; }
        public string ApplicationUrlWebApp { get; set; }
        public string ApplicationMaximumLoginAttempt { get; set; }
        public string ApplicationPageLength { get; set; }
        public string ApplicationClientId { get; set; }

        public string JwtKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtExpiresInDays { get; set; }


        public string SecurityEncryptKeyBase64 { get; set; }
        public string SecurityEncryptKeyMD5 { get; set; }

        public string FileUrl { get; set; }
        public string FileMaximumFileSize { get; set; }
        public string FileAllowedFileExtensions { get; set; }

        public string FirebaseProjectId { get; set; }
        public string FirebaseType { get; set; }
        public string FirebasePrivateKeyId { get; set; }
        public string FirebasePrivateKey { get; set; }
        public string FirebaseClientEmail { get; set; }
        public string FirebaseClientId { get; set; }
        public string FirebaseAuthUri { get; set; }
        public string FirebaseTokenUri { get; set; }
        public string FirebaseAuthProviderX509CertUrl { get; set; }
        public string FirebaseClientX509CertUrl { get; set; }
        public string FirebaseUniverseDomain { get; set; }

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
        public string YoloLambMinimumAreaRatio { get; set; }
        public string YoloLambPartAreaRatio { get; set; }
        public string YoloApiUrl { get; set; }
        public string YoloClientId { get; set; }
        public string YoloClientSecret { get; set; }

        public string Roles { get; set; }
    }
}
