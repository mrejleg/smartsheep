using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;

namespace Web.Controllers.Reports
{
    public class SucklingStatisticReportController : BaseWebController
    {
        private readonly ISmartSheepReportService _service;
        private readonly IMasterDataRepositories _masterDataRepositories;
        private string viewPath;

        public SucklingStatisticReportController(INotyfService notyfService, AppSetting appSetting, ISmartSheepReportService service, IMasterDataRepositories masterDataRepositories) : base(notyfService, appSetting)
        {
            _service = service;
            _masterDataRepositories = masterDataRepositories;
            viewPath = "~/Views/Reports/SucklingStatisticReport/";
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page = 1)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            from ??= DateTime.Today.AddDays(-7);
            to ??= DateTime.Today;
            var queryTo = to.Value.Date.AddDays(1).AddTicks(-1);

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");
            var farms = await _masterDataRepositories.Farms.GetAllAsync();
            var barns = await _masterDataRepositories.Barns.GetAllAsync();
            ViewBag.FarmId = farmId;
            ViewBag.BarnId = barnId;
            ViewBag.Farms = (farms.Data ?? []).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
            ViewBag.Barns = (barns.Data ?? []).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
            const int pageSize = 20;
            page = Math.Max(page, 1);
            var result = await _service.GetSucklingStatisticsAsync(from.Value.Date, queryTo, farmId, barnId, page, pageSize);
            var data = result.Data ?? new List<Web.Domain.Models.SmartSheep.SucklingStatistic>();
            ViewBag.Page = page;
            ViewBag.HasNext = data.Count > pageSize;
            return View(viewPath + "Index.cshtml", data.Take(pageSize).ToList());
        }
    }
}
