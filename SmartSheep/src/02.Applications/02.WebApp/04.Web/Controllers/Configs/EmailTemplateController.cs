using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.Configs;

namespace Web.Controllers.Configs
{
    public class EmailTemplateController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public EmailTemplateController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/Configs/EmailTemplate/";
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.EmailTemplates.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<EmailTemplate>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<EmailTemplate> model, string bodyType)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                return View(viewPath + "Create.cshtml", model);
            }

            if (bodyType == "html")
            {
                model.Data.IsHtmlTemplate = true;
                model.Data.Body = model.Data.HtmlBody;
            }
            else
            {
                model.Data.Body = model.Data.TextBody;
            }

            var result = await _configRepositories.EmailTemplates.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _configRepositories.EmailTemplates.GetAsync(id);
            if (result.Data.IsHtmlTemplate)
            {
                result.Data.BodyType = "html";
                result.Data.HtmlBody = result.Data.Body;
            }
            else
            {
                result.Data.TextBody = result.Data.Body;
            }

            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<EmailTemplate> model, string bodyType)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);

                return View(viewPath + "Edit.cshtml", model);
            }

            if (bodyType == "html")
            {
                model.Data.IsHtmlTemplate = true;
                model.Data.Body = model.Data.HtmlBody;
            }
            else
            {
                model.Data.Body = model.Data.TextBody;
            }

            var result = await _configRepositories.EmailTemplates.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _configRepositories.EmailTemplates.GetAsync(id);
            if (result.Data.IsHtmlTemplate)
            {
                result.Data.BodyType = "html";
                result.Data.HtmlBody = result.Data.Body;
            }
            else
            {
                result.Data.TextBody = result.Data.Body;
            }

            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _configRepositories.EmailTemplates.DeleteAsync(id);
            return Delete(result);
        }
    }
}
