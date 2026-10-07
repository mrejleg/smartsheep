namespace Project.Base.Models.Notifications
{
    public class SendEmailParam
    {
        public List<EmailContact> To { get; set; } = new List<EmailContact>();

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string IsUseEmail { get; set; }

        public string Url { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;
    }
}
