namespace Project.Base.Models.RestApis
{
    public class RestApiResponse
    {
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public void SetRestApiResponse(int statusCode, string statusText, string message, object data)
        {
            StatusCode = statusCode;
            StatusText = statusText;
            Message = message;
            Data = data;
        }
    }
}
