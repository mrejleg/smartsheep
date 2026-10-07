namespace Project.Base.Models.Jwts
{
    public class JwtRequest
    {
        public string? JwtKey { get; set; }
        public string? JwtIssuer { get; set; }
        public DateTime JwtExpireIn { get; set; }
        public string? ApplicationId { get; set; }
        public string? ApplicationName { get; set; }
        public string? ApplicationCode { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PositionId { get; set; }
        public string? PositionName { get; set; }
        public string? Roles { get; set; }
        public string? CostCenterCode { get; set; }
        public string? EaCode { get; set; }
    }
}
