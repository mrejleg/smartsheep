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
    public class SucklingActivityController : BaseWebController
    {
        private readonly ISmartSheepRepositories _smartSheepRepositories;
        private string viewPath;

        public SucklingActivityController(INotyfService notyfService, AppSetting appSetting, ISmartSheepRepositories smartSheepRepositories) : base(notyfService, appSetting)
        {
            _smartSheepRepositories = smartSheepRepositories;
            viewPath = "~/Views/SmartSheep/SucklingActivity/";
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _smartSheepRepositories.SucklingActivities.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<SucklingActivity>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<SucklingActivity> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _smartSheepRepositories.SucklingActivities.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingActivities.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<SucklingActivity> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _smartSheepRepositories.SucklingActivities.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingActivities.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _smartSheepRepositories.SucklingActivities.DeleteAsync(id);
            return Delete(result);
        }
    }
}

