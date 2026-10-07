using Project.Base.Models.Entities;
using Project.Base.Models.JQueries.Select2s;

namespace Web.Domain.Models.Configs
{
    public class AppMenu : BaseEntity<Guid>
    {
        public AppMenu? AppMenuParent { get; set; }
        public Guid? FkParentId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Controller { get; set; }

        public string SequenceNumber { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string? IconActive { get; set; }

        public bool IsSection { get; set; }

        public bool IsMobile { get; set; }

        public string AccessTypes { get; set; } = string.Empty;

        public List<Select2Binding>? AppMenuAccessTypes { get; set; } = new List<Select2Binding>();
    }
}
