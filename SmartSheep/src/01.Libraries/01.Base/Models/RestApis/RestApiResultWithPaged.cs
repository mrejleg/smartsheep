namespace Project.Base.Models.RestApis
{
    public class RestApiResultWithPaged<TEntity>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public TEntity Data { get; set; }
        public int Length { get; set; }
        public int TotalSize { get; set; }
        public int Page { get; set; }
        public int PageCount { get; set; }
    }
}
