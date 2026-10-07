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
    public class BarnController : BaseWebController
    {
        private readonly IMasterDataRepositories _masterDataRepositories;
        private string viewPath;

        public BarnController(INotyfService notyfService, AppSetting appSetting, IMasterDataRepositories masterDataRepositories) : base(notyfService, appSetting)
        {
            _masterDataRepositories = masterDataRepositories;
            viewPath = "~/Views/MasterData/Barn/";
        }

        public async Task<IList<Barn>> BarnsAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.Barns.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.Barns.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<Barn>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<Barn> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _masterDataRepositories.Barns.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _masterDataRepositories.Barns.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<Barn> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _masterDataRepositories.Barns.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _masterDataRepositories.Barns.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _masterDataRepositories.Barns.DeleteAsync(id);
            return Delete(result);
        }
    }
}

