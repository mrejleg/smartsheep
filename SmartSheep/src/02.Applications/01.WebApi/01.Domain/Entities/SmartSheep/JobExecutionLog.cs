using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.Entities.SmartSheep
{
    public class JobExecutionLog : BaseEntity<Guid>
    {
        public DateTime? ExecutedAt { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Succeeded: job completed successfully; Failed: job execution failed.
        public string Status { get; set; } = "Succeeded";

        [Column(TypeName = "VARCHAR(2000)")]
        [StringLength(2000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string? StatusNotes { get; set; }
    }
}
