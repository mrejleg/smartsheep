namespace Web.Domain.Models.ApiManagements
{
    public class AppScopeClientDto : AppScopeClient
    {
        public List<ScopeClient>? ScopeClients { get; set; }
        public List<AppScopeClient>? AppScopeClients { get; set; }
    }
}
