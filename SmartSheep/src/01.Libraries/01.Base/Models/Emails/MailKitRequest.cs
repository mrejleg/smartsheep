namespace Project.Base.Models.Emails
{
    public class MailKitRequest
    {
        public string SmtpHost { get; set; }
        public string SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string EmailFrom { get; set; }
        public string EmailFromName { get; set; }
        public string SecureOption { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsEnableSsl { get; set; }

        public string Name { get; set; }
        public string EmailTo { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
