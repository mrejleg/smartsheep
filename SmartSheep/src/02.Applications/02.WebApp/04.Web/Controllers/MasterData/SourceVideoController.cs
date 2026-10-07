using Microsoft.AspNetCore.Mvc;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.Domain.Models;
using Web.Domain.Models.MasterDatas;

namespace Web.Controllers.MasterData
{
    public class SourceVideoController : BaseWebController
    {
        // Sama dengan default periode statistik di API bila SystemConfig belum diisi
        private const int DefaultStatisticsDurationMinutes = 120;

        private readonly IMasterDataRepositories _masterDataRepositories;
        private readonly IConfigRepositories _configRepositories;
        private readonly ISourceVideoStreamService _sourceVideoStreamService;
        private readonly ISourceVideoInferenceService _sourceVideoInferenceService;
        private string viewPath;

        public SourceVideoController(INotyfService notyfService, AppSetting appSetting, IMasterDataRepositories masterDataRepositories,
            IConfigRepositories configRepositories, ISourceVideoStreamService sourceVideoStreamService, ISourceVideoInferenceService sourceVideoInferenceService) : base(notyfService, appSetting)
        {
            _masterDataRepositories = masterDataRepositories;
            _configRepositories = configRepositories;
            _sourceVideoStreamService = sourceVideoStreamService;
            _sourceVideoInferenceService = sourceVideoInferenceService;
            viewPath = "~/Views/MasterData/SourceVideo/";
        }

        public async Task<IList<SourceVideo>> SourceVideosAsync()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.SourceVideos.GetAllAsync();
            return data.Data.Where(x => x.IsActive == true).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetDataDxGrid([FromQuery] DataSourceLoadOptions loadOptions)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var data = await _masterDataRepositories.SourceVideos.DxGridAsync(loadOptions);
            return DataDxGrid(data);
        }

        public ActionResult Index()
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            return View(viewPath + "Index.cshtml");
        }

        public IActionResult Create()
        {
            return Create<SourceVideo>(viewPath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync(RestApiResult<SourceVideo> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Create.cshtml", model);
            }

            var result = await _masterDataRepositories.SourceVideos.PostAsync(model.Data);
            return Create(viewPath, model, result);
        }

        public async Task<IActionResult> EditAsync(Guid id)
        {
            var result = await _masterDataRepositories.SourceVideos.GetAsync(id);
            return Edit(viewPath, id, result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAsync(RestApiResult<SourceVideo> model)
        {
            if (!ModelState.IsValid || model == null)
            {
                _notyfService.Error(Messages.ModelStateInvalidMessage);
                return View(viewPath + "Edit.cshtml", model);
            }

            var result = await _masterDataRepositories.SourceVideos.PutAsync(model.Data);
            return Edit(viewPath, model, result);
        }

        public async Task<IActionResult> DetailsAsync(Guid id)
        {
            var result = await _masterDataRepositories.SourceVideos.GetAsync(id);
            return Details(viewPath, id, result);
        }

        public async Task<IActionResult> StreamAsync(Guid id)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var result = await _masterDataRepositories.SourceVideos.GetAsync(id);
            ViewBag.EmbedUrl = result.Data == null ? null : _sourceVideoStreamService.GetEmbedUrl(result.Data.SourceVideoUrl);
            ViewBag.PreviewId = Guid.NewGuid();
            ViewBag.EpisodeMinutes = await GetStatisticsDurationMinutesAsync();
            return View(viewPath + "Stream.cshtml", result);
        }

        // Episode menyusu = periode statistik SystemConfig aktif terbaru (menit sejak 00.00), seperti di API
        private async Task<int> GetStatisticsDurationMinutesAsync()
        {
            var configs = await _configRepositories.SystemConfigs.GetAllAsync();
            int minutes = configs.Data?
                .Where(x => x.IsActive == true)
                .OrderByDescending(x => x.DateModified ?? x.DateCreated)
                .Select(x => x.StatisticsDurationMinutes)
                .FirstOrDefault() ?? 0;
            return minutes > 0 ? minutes : DefaultStatisticsDurationMinutes;
        }

        // MJPEG beranotasi hasil preview inferensi YOLO untuk <img>; berhenti otomatis saat halaman ditutup
        [HttpGet]
        public async Task StreamFramesAsync(Guid id, Guid previewId)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            var result = await _masterDataRepositories.SourceVideos.GetAsync(id);
            if (result.Data == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            Response.Headers.CacheControl = "no-cache, no-store";
            try
            {
                await _sourceVideoInferenceService.WriteFramesAsync(result.Data, previewId, Response.Body, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Halaman streaming ditutup
            }
        }

        // Statistik live preview (tidak disimpan; data resmi dihitung worker)
        [HttpGet]
        public IActionResult StreamStatistic(Guid previewId)
        {
            AuthorizePage(AccessTypes.View.GetEnumDescriptionByValue());

            Response.Headers.CacheControl = "no-cache, no-store";
            return Json(_sourceVideoInferenceService.GetStatistic(previewId));
        }

        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _masterDataRepositories.SourceVideos.DeleteAsync(id);
            return Delete(result);
        }
    }
}

