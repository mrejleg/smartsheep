using System.Text.Json.Serialization;

namespace Project.Base.Models.Notifications
{
    public class SendEmailResponse
    {
        public string MessageId { get; set; } = string.Empty;
    }
}
