namespace Project.Base.Models.Jwts
{
    public class TokenJwtRequest : TokenRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? Roles { get; set; }
    }
}
