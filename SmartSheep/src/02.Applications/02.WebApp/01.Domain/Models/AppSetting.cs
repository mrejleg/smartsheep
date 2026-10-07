namespace Web.Domain.Models
{
    public class AppSetting
    {
        public string ApplicationUrlPublish { get; set; }
        public string ApplicationIsUnderMaintenance { get; set; }
        public string ApplicationIsHttpsRequired { get; set; }
        public string ApplicationCookieName { get; set; }
        public string ApplicationCookieExpiresInDays { get; set; }
        public string ApplicationUrl { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationClientId { get; set; }
        public string ApplicationIsByPassWhitelist { get; set; }
        public string ApplicationCompanyCode { get; set; }

        public string SecurityEncryptKeyBase64 { get; set; }
        public string SecurityEncryptKeyMD5 { get; set; }

        public string ApiUrl { get; set; }
        public string ApiClientId { get; set; }
        public string ApiClientSecret { get; set; }
        public string ApiCookiePrefix { get; set; }

        // Preview inferensi YOLO di halaman streaming SourceVideo (aturan dari section Yolo appsettings API)
        public string YoloAppSettings { get; set; }
        public string YoloModel { get; set; }
        public string YoloLabels { get; set; }
        public string YoloUseCoreMl { get; set; }
        public string YoloPendingFolder { get; set; }

        public string Roles { get; set; }
    }
}
