namespace Api.Domain.Models.Reports
{
    public class SmartSheepReportFilterParam
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? Hour { get; set; }
        public Guid? BarnId { get; set; }
        public Guid? FarmId { get; set; }
        public bool? RequiresReminder { get; set; }
        public string? Status { get; set; }
    }
}
