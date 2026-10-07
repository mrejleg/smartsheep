using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.ApiManagements;

namespace Web.Controllers.ApiManagements
{
    public class ScopeClientController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public ScopeClientController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/ApiManagements/ScopeClient/";
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.ScopeClients.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<ScopeClient>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<ScopeClient> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _configRepositories.ScopeClients.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _configRepositories.ScopeClients.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<ScopeClient> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _configRepositories.ScopeClients.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _configRepositories.ScopeClients.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _configRepositories.ScopeClients.DeleteAsync(id);
            return Delete(result);
        }
    }
}
