namespace Project.Core.Extentions
{
    public static class BooleanExtensions
    {
        public static string ToYesNoDisplayText(this bool value)
        {
            return value ? "Yes" : "No";
        }

        public static string ToActiveInactiveDisplayText(this bool value)
        {
            return value ? "Active" : "Inactive";
        }

        public static string ToEnabledDisabledDisplayText(this bool value)
        {
            return value ? "Enabled" : "Disabled";
        }
    }
}
