using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Domain.ApiManagements
{
    public class AppScopeClient : BaseEntity<Guid>
    {
        [ForeignKey("FkAppClientId")]
        public AppClient? AppClient { get; set; }
        public Guid? FkAppClientId { get; set; }

        [ForeignKey("FkScopeClientId")]
        public ScopeClient? ScopeClient { get; set; }
        public Guid? FkScopeClientId { get; set; }
    }
}
