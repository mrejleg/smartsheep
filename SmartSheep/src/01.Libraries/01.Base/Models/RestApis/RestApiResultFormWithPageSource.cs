namespace Project.Base.Models.RestApis
{
    public class RestApiResultFormWithPageSource<TEntity> where TEntity : new()
    {
        public bool IsEdit { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public string Page { get; set; }
        public int StatusCode { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
        public TEntity Data { get; set; } = new TEntity();
        public string PageSource { get; set; }
    }
}
