using Project.Core.Toasts.Notyf.Enums;

namespace Project.Core.Toasts.Notyf.Models
{
    public class NotyfConfig
    {
        public int DurationInSeconds { get; set; }
        public NotyfPosition Position { get; set; } = NotyfPosition.BottomRight;
        public bool IsDismissable { get; set; } = false;
        public bool HasRippleEffect { get; set; } = true;
    }
}
