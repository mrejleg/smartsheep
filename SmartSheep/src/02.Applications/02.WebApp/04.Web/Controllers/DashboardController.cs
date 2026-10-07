using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Dashboards;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.Dashboards;

namespace Web.Controllers
{
    public class DashboardController : BaseWebController
    {
        private readonly IDashboardService _service;
        private readonly IMasterDataRepositories _masterDataRepositories;

        public DashboardController(INotyfService notyfService, AppSetting appSetting, IDashboardService service, IMasterDataRepositories masterDataRepositories) : base(notyfService, appSetting)
        {
            _service = service;
            _masterDataRepositories = masterDataRepositories;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, Guid? farmId, Guid? barnId)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());
            startDate ??= DateTime.Today.AddDays(-7);
            endDate ??= DateTime.Today;

            var response = await _service.GetAsync(startDate, endDate, farmId, barnId);
            var farms = await _masterDataRepositories.Farms.GetAllAsync();
            var barns = await _masterDataRepositories.Barns.GetAllAsync();

            ViewBag.Farms = farms.Data.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.Barns = barns.Data.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.FarmId = farmId;
            ViewBag.BarnId = barnId;

            return View(response.Data ?? new DashboardDataModel { Date = startDate.Value.Date });
        }
    }
}
