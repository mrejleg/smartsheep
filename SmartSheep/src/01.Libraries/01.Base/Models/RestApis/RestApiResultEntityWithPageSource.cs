namespace Project.Base.Models.RestApis
{
    public class RestApiResultWithPageSource<TEntity> where TEntity : new()
    {
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
        public TEntity Data { get; set; } = new TEntity();
        public string PageSource { get; set; }
    }
}
