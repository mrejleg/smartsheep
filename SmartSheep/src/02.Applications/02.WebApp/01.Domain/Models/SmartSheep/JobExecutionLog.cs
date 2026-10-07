using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.SmartSheep
{
    public class JobExecutionLog : BaseEntity<Guid>
    {
        public DateTime? ExecutedAt { get; set; }

        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 1)]
        // Succeeded: job completed successfully; Failed: job execution failed.
        public string Status { get; set; } = "Succeeded";

        [StringLength(2000, MinimumLength = 1)]
        public string? StatusNotes { get; set; }
    }
}
