using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.MasterDatas;

namespace Web.Controllers.MasterData
{
    public class LivestockController : BaseWebController
    {
        private readonly IMasterDataRepositories _masterDataRepositories;
        private string viewPath;

        public LivestockController(INotyfService notyfService, AppSetting appSetting, IMasterDataRepositories masterDataRepositories) : base(notyfService, appSetting)
        {
            _masterDataRepositories = masterDataRepositories;
            viewPath = "~/Views/MasterData/Livestock/";
        }

        public async Task<IList<Livestock>> LivestocksAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.Livestocks.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.Livestocks.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<Livestock>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<Livestock> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _masterDataRepositories.Livestocks.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _masterDataRepositories.Livestocks.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<Livestock> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _masterDataRepositories.Livestocks.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _masterDataRepositories.Livestocks.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _masterDataRepositories.Livestocks.DeleteAsync(id);
            return Delete(result);
        }
    }
}

