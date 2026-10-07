namespace Web.Domain.Constants
{
    public static class MenuDictionary
    {
        private static readonly List<string> MasterMenus = new List<string>
        {
            "Farm",
            "Barn",
            "Livestock",
            "SourceVideo"
        };

        public static bool IsMasterDataMenu(string menu)
        {
            return MasterMenus.Contains(menu);
        }
    }
}
