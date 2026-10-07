using DevExtreme.AspNet.Data;

namespace Project.Base.Models.DevExpress.DxGrids
{
    public class DataSourceLoadOptions : DataSourceLoadOptionsBase
    {
        public new string? Sort { get; set; }
        public new string? Group { get; set; }
        public new string? Filter { get; set; }
        public new string? TotalSummary { get; set; }
        public new string? GroupSummary { get; set; }
        public new string? Select { get; set; }
        public new string? PreSelect { get; set; }
        public new string? PrimaryKey { get; set; }
        public string? FilterParam1 { get; set; }
        public string? FilterParam2 { get; set; }
        public string? FilterParam3 { get; set; }
        public string? FilterParam4 { get; set; }
        public string? FilterParam5 { get; set; }
        public string? FilterParam6 { get; set; }
    }
}
