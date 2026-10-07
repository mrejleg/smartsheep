namespace Project.Base.Models
{
    public class DataPagedResults<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalSize { get; set; }
    }
}
