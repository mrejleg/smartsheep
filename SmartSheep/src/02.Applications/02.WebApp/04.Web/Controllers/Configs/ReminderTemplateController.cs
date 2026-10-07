using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using System.Net;
using Web.ApplicationCore.Interfaces.Auths;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

namespace Web.Controllers.Configs
{
    public class ReminderTemplateController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private readonly IAuthService _authService;
        private string viewPath;

        public ReminderTemplateController(
            INotyfService notyfService,
            AppSetting appSetting,
            IConfigRepositories configRepositories,
            IAuthService authService) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            _authService = authService;
            viewPath = "~/Views/Configs/ReminderTemplate/";
        }

        public async Task<IList<ReminderTemplate>> ReminderTemplatesAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.ReminderTemplates.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.ReminderTemplates.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<ReminderTemplate>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<ReminderTemplate> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _configRepositories.ReminderTemplates.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _configRepositories.ReminderTemplates.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<ReminderTemplate> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _configRepositories.ReminderTemplates.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _configRepositories.ReminderTemplates.GetAsync(id);
            return Details(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestReminderAsync(Guid id)
        {
            var editAccess = AccessTypes.Edit.GetEnumDescriptionByValue();
            if (!await _authService.GetAccessMenuAsync("ReminderTemplate", editAccess))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    Success = false,
                    Message = "You do not have permission to test reminder templates."
                });
            }

            var result = await _configRepositories.ReminderTemplates.TestAsync(id);
            var statusCode = Enum.IsDefined(typeof(HttpStatusCode), result.StatusCode)
                ? result.StatusCode
                : StatusCodes.Status500InternalServerError;

            return StatusCode(statusCode, new
            {
                Success = result.StatusCode == StatusCodes.Status200OK,
                result.Message,
                result.Data
            });
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _configRepositories.ReminderTemplates.DeleteAsync(id);
            return Delete(result);
        }
    }
}
