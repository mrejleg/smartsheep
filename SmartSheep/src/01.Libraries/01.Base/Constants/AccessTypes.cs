using System.ComponentModel;

namespace Project.Base.Constants
{
    public enum AccessTypes
    {
        [field: Description("view")]
        View,

        [field: Description("add")]
        Add,

        [field: Description("edit")]
        Edit,

        [field: Description("delete")]
        Delete,

        [field: Description("detail")]
        Detail,

        [field: Description("save")]
        Save,

        [field: Description("submit")]
        Submit,

        [field: Description("approval")]
        Approval
    }
}
