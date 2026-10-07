using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using System.Data;
using System.Net;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

namespace Web.Controllers.Configs
{
    public class AppMenuController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public AppMenuController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/Configs/AppMenu/";
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.AppMenus.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public async Task<IList<AppMenu>> AppMenuParentsAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.AppMenus.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true && x.FkParentId == null && x.IsSection == false).ToList();
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public async Task<IActionResult> CreateAsync()
        {
            AuthorizePage(AccessTypes.Add.GetEnumDescriptionByValue());

            var model = new RestApiResult<AppMenu>();
            var accessTypes = await _configRepositories.AppMenus.BindingSelect2AccessTypes();
            model.Data.AppMenuAccessTypes = accessTypes.Data;
            return View(viewPath + "Create.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<AppMenu> model)
        {
            var accessTypeResults = await _configRepositories.AppMenus.BindingSelect2AccessTypes();

            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                if (accessTypeResults.StatusCode == (int)HttpStatusCode.OK)
                {
                    model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                }
                return View(viewPath + "Create.cshtml", model);
            }

            var accessTypes = new List<string>();
            foreach (var accessType in accessTypeResults.Data)
            {
                var form = Request.Form["chk_" + accessType.Id];
                if (!string.IsNullOrEmpty(form))
                {
                    accessTypes.Add(accessType.Id.ToString().ToLower());
                }
            }

            model.Data.AccessTypes = string.Join(",", accessTypes);

            if (accessTypes.Count == 0)
            {
                _notyfService.Error("Access types can not empty");
                model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _configRepositories.AppMenus.PostAsync(model.Data);
            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(result.Message);
                model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                return View(viewPath + "Create.cshtml", model);
            }

            _notyfService.Success(Messages.SuccessSaveMessage);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var model = await _configRepositories.AppMenus.GetAsync(id);
            if (model.StatusCode == (int)HttpStatusCode.OK)
            {
                if (model.Data != null)
                {
                    var accessTypes = await _configRepositories.AppMenus.BindingSelect2AccessTypes();
                    model.Data.AppMenuAccessTypes = accessTypes.Data;
                    return View(viewPath + "Edit.cshtml", model);
                }
            }

            _notyfService.Error(Messages.RequiredDataMessage);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<AppMenu> model)
        {
            var accessTypeResults = await _configRepositories.AppMenus.BindingSelect2AccessTypes();

            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                if (accessTypeResults.StatusCode == (int)HttpStatusCode.OK)
                {
                    model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                }
                return View(viewPath + "Edit.cshtml", model);
            }

            var accessTypes = new List<string>();
            foreach (var accessType in accessTypeResults.Data)
            {
                var form = Request.Form["chk_" + accessType.Id];
                if (!string.IsNullOrEmpty(form))
                {
                    accessTypes.Add(accessType.Id.ToString().ToLower());
                }
            }

            model.Data.AccessTypes = string.Join(",", accessTypes);

            if (accessTypes.Count == 0)
            {
                _notyfService.Error("Access types can not empty");
                model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _configRepositories.AppMenus.PutAsync(model.Data);
            if (result.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(result.Message);


                model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                return View(viewPath + "Edit.cshtml", model);
            }

            _notyfService.Success(Messages.SuccessSaveMessage);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var model = await _configRepositories.AppMenus.GetAsync(id);
            if (model.StatusCode == (int)HttpStatusCode.OK)
            {
                if (model.Data != null)
                {
                    var accessTypeResults = await _configRepositories.AppMenus.BindingSelect2AccessTypes();

                    model.Data.AppMenuAccessTypes = accessTypeResults.Data;
                    return View(viewPath + "Details.cshtml", model);
                }
            }

            _notyfService.Error(Messages.RequiredDataMessage);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var model = await _configRepositories.AppMenus.GetAsync(id);
            if (model.StatusCode != (int)HttpStatusCode.OK)
            {
                _notyfService.Error(model.Message);
            }
            else
            {
                var result = await _configRepositories.AppMenus.DeleteAsync(id);

                if (result.StatusCode != (int)HttpStatusCode.OK)
                {
                    _notyfService.Error(result.Message);
                }
                else
                {
                    _notyfService.Success(Messages.SuccessDeleteMessage);
                }
            }

            return RedirectToAction("Index");
        }
    }
}
