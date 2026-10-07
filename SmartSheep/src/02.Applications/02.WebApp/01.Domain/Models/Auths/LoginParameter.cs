namespace Web.Domain.Models.Auths
{
    public class LoginParameter
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string MessageError { get; set; }
        public string UrlAction { get; set; }
        public bool RememberMe { get; set; }
    }
}
