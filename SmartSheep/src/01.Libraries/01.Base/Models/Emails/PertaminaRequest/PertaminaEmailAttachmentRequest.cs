namespace Project.Base.Models.Emails.PertaminaRequest
{
    public class PertaminaEmailAttachmentRequest : PertaminaEmailRequest
    {
        public IDictionary<string, byte[]> Attachment { get; set; } = new Dictionary<string, byte[]>();
    }
}
