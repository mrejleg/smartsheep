using Project.Base.Constants;

namespace Project.Base.Models.JQueries.Datatables
{
    public class DatatablesOrder
    {
        public DatatablesOrder() { }
        public int Column { get; set; }
        public OrderTypes Dir { get; set; }
    }
}
