using Project.Base.Models.Entities;

namespace Web.Domain.Models.ApiManagements
{
    public class AppScopeClient : BaseEntity<Guid>
    {
        public AppClient? AppClient { get; set; }
        public Guid? FkAppClientId { get; set; }

        public ScopeClient? ScopeClient { get; set; }
        public Guid? FkScopeClientId { get; set; }
    }
}
