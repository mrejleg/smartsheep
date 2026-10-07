using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;
using Web.Domain.Models.MasterDatas;

namespace Web.Domain.Models.SmartSheep
{
    public class SourceVideoStatusLog : BaseEntity<Guid>
    {
        public SourceVideo? SourceVideo { get; set; }
        public Guid? FkIdSourceVideo { get; set; }

        [Required, StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        public DateTime? LoggedAt { get; set; }

        [Required, StringLength(20, MinimumLength = 1)]
        // Online: accessible; Offline: not broadcasting; Inaccessible: connection attempt failed.
        public string Status { get; set; } = "Offline";
    }
}
