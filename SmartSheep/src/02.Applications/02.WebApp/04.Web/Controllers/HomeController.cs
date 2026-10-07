using Microsoft.AspNetCore.Mvc;
using Project.Core.Toasts.Abstractions;
using Web.Domain.Models;

namespace Web.Controllers
{
    public class HomeController : BaseWebController
    {
        public HomeController(INotyfService notyfService, AppSetting appSetting) : base(notyfService, appSetting)
        {
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}