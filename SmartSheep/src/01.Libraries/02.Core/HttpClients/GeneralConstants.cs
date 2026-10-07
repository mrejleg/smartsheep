namespace Project.Core.HttpClients
{
    public static class GeneralConstants
    {
        public const string AccessTokenName = "access_token";
        public const string RefreshTokenName = "refresh_token";
        public const string IsRenewalToken = "IsRenewalToken";
        public const string DefaultAuthenticateScheme = "Cookies";
        public const string OAuthName = "OAuth";
        public const string RoutingToken = "Auth/Token";
        public const string HeaderScheme = "Bearer";
        public const string EncryptKeyName = "EncryptKeyMD5";
        public const string Source = "SourceClient";
        public const string ApplicationCookieName = "ApplicationCookieName";
        public const string ApplicationCookieExpiresInDays = "ApplicationCookieExpiresInDays";

        public const string ActionNameLogout = "Logout";
        public const string ControllerNameLogout = "Account";
        public const string ViewNameUnauthorize = "_NotAuthorize";
    }
}
