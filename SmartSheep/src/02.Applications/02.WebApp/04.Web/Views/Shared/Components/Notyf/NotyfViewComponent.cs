using Microsoft.AspNetCore.Mvc;
using Project.Core.Toasts.Abstractions;
using Project.Core.Toasts.Helpers;
using Project.Core.Toasts.Notyf;
using Project.Core.Toasts.Notyf.Models;

namespace Web.Views.Shared.Components.Notyf
{
    [ViewComponent(Name = "Notyf")]
    public class NotyfViewComponent : ViewComponent
    {
        private readonly INotyfService _service;

        public NotyfViewComponent(INotyfService service, NotyfEntity options)
        {
            _service = service;
            _options = options;
        }

        public NotyfEntity _options { get; }

        public IViewComponentResult Invoke()
        {
            var model = new NotyfViewModel
            {
                Configuration = _options.ToJson(),
                Notifications = _service.ReadAllNotifications()
            };

            return View("Default", model);
        }
    }
}
