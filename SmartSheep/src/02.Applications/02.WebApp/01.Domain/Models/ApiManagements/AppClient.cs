using Project.Base.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Web.Domain.Models.ApiManagements
{
    public class AppClient : BaseEntity<Guid>
    {
        public string? Name { get; set; }

        public string? ClientId { get; set; }

        [DataType(DataType.Password)]
        public string? ClientSecret { get; set; }

        public string? Description { get; set; }

        public double ExpiredInSecond { get; set; }
    }
}
