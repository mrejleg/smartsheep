namespace Project.Base.Models.RestApis
{
    public class RestApiResultFile
    {
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
    }
}
