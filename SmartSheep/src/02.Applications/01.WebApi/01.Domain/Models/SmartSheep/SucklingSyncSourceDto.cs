namespace Api.Domain.Models.SmartSheep
{
    // Sumber video untuk InferenceYolo (dicari dari SourceVideo.Code) beserta kandangnya.
    public class SucklingSyncSourceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string BarnCode { get; set; } = string.Empty;
        public string SourceVideoUrl { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool IsOnline { get; set; }
    }
}
