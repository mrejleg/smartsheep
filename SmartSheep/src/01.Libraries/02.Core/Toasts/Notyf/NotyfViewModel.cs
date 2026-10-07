using Project.Core.Toasts.Notyf.Models;

namespace Project.Core.Toasts.Notyf
{
    public class NotyfViewModel
    {
        public string Configuration { get; set; }
        public IEnumerable<NotyfNotification> Notifications { get; set; }
    }
}
