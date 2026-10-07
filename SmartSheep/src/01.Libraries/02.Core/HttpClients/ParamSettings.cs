namespace Project.Core.HttpClients
{
    public class ParamSettings
    {
        public string ApplicationCookieName { get; set; }
        public string ApplicationCookieExpiresInDays { get; set; }

        public string SecurityEncryptKeyBase64 { get; set; }
        public string SecurityEncryptKeyMD5 { get; set; }

        public string ApiUrl { get; set; }
        public string ApiClientId { get; set; }
        public string ApiClientSecret { get; set; }
        public string ApiCookiePrefix { get; set; }

        public string? Roles { get; set; }
    }
}
