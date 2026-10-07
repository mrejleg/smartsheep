using Microsoft.AspNetCore.Mvc;
using Project.Core.Toasts.Abstractions;
using Web.Domain.Models;

namespace Web.Controllers.SmartSheep;

public class SheepBreedingController : BaseWebController
{
    public SheepBreedingController(INotyfService notyfService, AppSetting appSetting) : base(notyfService, appSetting) { }

    public IActionResult Index()
    {
        AuthorizePage("view");
        return View("~/Views/SmartSheep/SheepBreeding/Index.cshtml");
    }
}
