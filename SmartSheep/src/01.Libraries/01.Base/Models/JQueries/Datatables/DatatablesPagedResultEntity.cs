namespace Project.Base.Models.JQueries.Datatables
{
    public class DatatablesPagedResultEntity<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalSize { get; set; }
    }
}
