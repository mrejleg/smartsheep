namespace Project.Base.Models.RestApis
{
    public class RestApiResult
    {
        public object Data { get; set; }
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
    }
}
