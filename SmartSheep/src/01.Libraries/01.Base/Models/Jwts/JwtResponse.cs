namespace Project.Base.Models.Jwts
{
    public class JwtResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
