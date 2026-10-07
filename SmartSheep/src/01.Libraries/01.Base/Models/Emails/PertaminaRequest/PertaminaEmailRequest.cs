namespace Project.Base.Models.Emails.PertaminaRequest
{
    public class PertaminaEmailRequest
    {
        public string IsUseEmail { get; set; }
        public string UrlEmail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string EmailTo { get; set; } = string.Empty;
        public string EmailCc { get; set; } = string.Empty;
        public string EmailBcc { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
