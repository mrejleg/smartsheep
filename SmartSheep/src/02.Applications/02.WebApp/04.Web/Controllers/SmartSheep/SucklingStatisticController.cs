using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.SmartSheep;

namespace Web.Controllers.SmartSheep
{
    public class SucklingStatisticController : BaseWebController
    {
        private readonly ISmartSheepRepositories _smartSheepRepositories;
        private string viewPath;

        public SucklingStatisticController(INotyfService notyfService, AppSetting appSetting, ISmartSheepRepositories smartSheepRepositories) : base(notyfService, appSetting)
        {
            _smartSheepRepositories = smartSheepRepositories;
            viewPath = "~/Views/SmartSheep/SucklingStatistic/";
        }

        public async Task<IList<SucklingStatistic>> SucklingStatisticsAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _smartSheepRepositories.SucklingStatistics.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _smartSheepRepositories.SucklingStatistics.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<SucklingStatistic>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<SucklingStatistic> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _smartSheepRepositories.SucklingStatistics.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingStatistics.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<SucklingStatistic> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _smartSheepRepositories.SucklingStatistics.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingStatistics.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingStatistics.DeleteAsync(id);
            return Delete(result);
        }
    }
}

