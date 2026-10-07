namespace Project.Base.Models.Firebases
{
    public sealed class ReminderTestResultDto
    {
        public Guid TemplateId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int RegisteredDeviceCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string FailureCode { get; set; } = string.Empty;
    }
}
