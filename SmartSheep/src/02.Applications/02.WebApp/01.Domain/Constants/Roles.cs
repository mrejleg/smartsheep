using System.ComponentModel;

namespace Web.Domain.Constants
{
    public enum Roles
    {
        [field: Description("SUPER.ADMIN")]
        SUPERADMIN,
        [field: Description("ALL.ACCESS")]
        ALLACCESS
    }
}
