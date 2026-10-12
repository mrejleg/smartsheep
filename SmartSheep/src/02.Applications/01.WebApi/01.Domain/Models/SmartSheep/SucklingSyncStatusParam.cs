namespace Api.Domain.Models.SmartSheep
{
    // Laporan InferenceYolo saat stream SourceVideo putus (IsOnline = false) atau tersambung lagi (IsOnline = true).
    public class SucklingSyncStatusParam
    {
        public string SourceVideoCode { get; set; } = string.Empty;

        public bool IsOnline { get; set; }
    }
}
