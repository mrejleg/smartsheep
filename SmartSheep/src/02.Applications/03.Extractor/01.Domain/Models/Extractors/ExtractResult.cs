namespace Extractor.Domain.Models.Extractors
{
    public class ExtractResult
    {
        // fps asli video (ffprobe: jumlah frame / durasi), dipakai untuk menghitung frame yang diambil
        public double Fps { get; set; }
        public int FrameCount { get; set; }
        public int ExpectedFrameCount { get; set; }
        public int Saved { get; set; }

        // Jumlah gambar yang namanya dari jam overlay (sisanya dihitung dari nama file video)
        public int NamedFromOverlay { get; set; }

        public bool RecordedAtFromFileName { get; set; }
    }
}
