namespace Extractor.Domain.Models.Extractors
{
    public class ExtractorOption
    {
        public string Input { get; set; } = "";
        public string Output { get; set; } = "frames";
        public double Interval { get; set; }          // wajib: detik antar gambar (1 = 1 gambar per detik)
        public string Ext { get; set; } = "jpg";
        public int Quality { get; set; } = 95;
        public int? Limit { get; set; }
        public bool SkipExisting { get; set; }
    }
}
