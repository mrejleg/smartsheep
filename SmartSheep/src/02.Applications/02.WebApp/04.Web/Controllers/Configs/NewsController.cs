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
    public class NewsController : BaseWebController
    {
        private readonly IConfigRepositories _configRepositories;
        private string viewPath;

        public NewsController(INotyfService notyfService, AppSetting appSetting, IConfigRepositories configRepositories) : base(notyfService, appSetting)
        {
            _configRepositories = configRepositories;
            viewPath = "~/Views/Configs/News/";
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _configRepositories.News.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<News>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<News> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            if (model.Data.ImageFile != null)
            {
                var upload = await _configRepositories.News.UploadImageAsync(model.Data.ImageFile);
                if (string.IsNullOrWhiteSpace(upload?.Data?.Path))
                {
                    _notyfService.Error(upload?.Message ?? "Failed to upload image thumbnail");
                    return View(viewPath + "Create.cshtml", model);
                }
                model.Data.ImageThumbnail = upload.Data.Path;
            }

            var result = await _configRepositories.News.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _configRepositories.News.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<News> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            if (model.Data.ImageFile != null)
            {
                var upload = await _configRepositories.News.UploadImageAsync(model.Data.ImageFile);
                if (string.IsNullOrWhiteSpace(upload?.Data?.Path))
                {
                    _notyfService.Error(upload?.Message ?? "Failed to upload image thumbnail");
                    return View(viewPath + "Edit.cshtml", model);
                }
                model.Data.ImageThumbnail = upload.Data.Path;
            }

            var result = await _configRepositories.News.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _configRepositories.News.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _configRepositories.News.DeleteAsync(id);
            return Delete(result);
        }
    }
}
