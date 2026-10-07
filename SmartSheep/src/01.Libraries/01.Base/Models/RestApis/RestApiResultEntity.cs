namespace Project.Base.Models.RestApis
{
    public class RestApiResult<TEntity> where TEntity : new()
    {
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
        public TEntity Data { get; set; } = new TEntity();
    }
}
