using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Domain.Entities.MasterDatas;

namespace Api.Domain.Entities.SmartSheep
{
    public class SourceVideoStatusLog : BaseEntity<Guid>
    {
        [ForeignKey("FkIdSourceVideo")]
        public SourceVideo? SourceVideo { get; set; }
        public Guid? FkIdSourceVideo { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(50)")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        public DateTime? LoggedAt { get; set; }

        [Required]
        [Column(TypeName = "VARCHAR(20)")]
        [StringLength(20, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        // Online: accessible; Offline: not broadcasting; Inaccessible: connection attempt failed.
        public string Status { get; set; } = "Offline";
    }
}
