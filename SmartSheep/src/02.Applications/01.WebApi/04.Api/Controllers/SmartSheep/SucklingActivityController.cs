using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.SmartSheep;
using Api.Domain.Models.SmartSheep;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.SmartSheep
{
    public class SucklingActivityController : BaseApiController
    {
        private readonly ISucklingActivityService _service;
        private readonly IAppLogger<SucklingActivity> _loger;
        private readonly ReminderDeliveryService _reminderDeliveryService;
        private readonly SourceVideoStatusService _sourceVideoStatusService;

        public SucklingActivityController(ISucklingActivityService service, IAppLogger<SucklingActivity> loger,
            ReminderDeliveryService reminderDeliveryService, SourceVideoStatusService sourceVideoStatusService)
        {
            _service = service;
            _loger = loger;
            _reminderDeliveryService = reminderDeliveryService;
            _sourceVideoStatusService = sourceVideoStatusService;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "SucklingActivity.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            return new OK("success get all data", data).ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "SucklingActivity.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            return new OK("Success get all data", data).ReturnResponse();
        }

        [HttpGet("GetAllByBarn/{fkBarnId}")]
        [AuthorizeFilter(Scope = "SucklingActivity.View")]
        public async Task<IActionResult> GetAllByBarn(Guid fkBarnId)
        {
            var data = await _service.GetAllByBarn(fkBarnId).AsNoTracking().ToListAsync();
            return new OK("Success get all data", data).ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "SucklingActivity.View")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var data = await _service.GetAsync(id);
            if (data == null)
            {
                _loger.LogError("Data not found : " + id);
                return new NotFound("Data not found : " + id, new { id }).ReturnResponse();
            }

            return new OK("Success get data by " + id, data).ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "SucklingActivity.View")]
        public async Task<IActionResult> GetAsync([FromBody] DataParameter param)
        {
            if (param.Start <= 0 || param.Length <= 0)
            {
                _loger.LogError("Bad request parameter. Start page > 0 and page size > 0");
                return new BadRequest("Start page > 0 and page size > 0", new { param.Start, param.Length }).ReturnResponse();
            }

            var data = await _service.GetAsync(param);
            return new OK("Success get data by paging", data).ReturnResponse();
        }

        [HttpPost]
        [AuthorizeFilter(Scope = "SucklingActivity.Add")]
        public async Task<IActionResult> PostAsync([FromBody] SucklingActivity data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");
                return new BadRequest("Parameter is null", data).ReturnResponse();
            }

            var id = await _service.PostAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data");

                var error = new Conflict("Duplicate data", data);
                return error.ReturnResponse();
            }

            var result = new OK("Success add data", new { id });
            return result.ReturnResponse();
        }

        [HttpGet("SyncSources")]
        [AuthorizeFilter(Scope = "SucklingActivity.Add")]
        public async Task<IActionResult> GetSyncSourcesAsync()
        {
            var data = await _service.GetSyncSourcesAsync();
            return new OK("Success get online source videos", data).ReturnResponse();
        }

        [HttpGet("SyncSource/{code}")]
        [AuthorizeFilter(Scope = "SucklingActivity.Add")]
        public async Task<IActionResult> GetSyncSourceAsync(string code)
        {
            var data = await _service.GetSyncSourceAsync(code);
            if (data == null)
            {
                _loger.LogError("Source video not found : " + code);
                return new NotFound("Source video not found : " + code, new { code }).ReturnResponse();
            }

            return new OK("Success get source video " + code, data).ReturnResponse();
        }

        [HttpPost("Sync")]
        [AuthorizeFilter(Scope = "SucklingActivity.Add")]
        public async Task<IActionResult> SyncAsync([FromBody] SucklingSyncParam param)
        {
            if (param == null || string.IsNullOrWhiteSpace(param.SourceVideoCode))
            {
                _loger.LogError("Bad request parameter. Source video code is required");
                return new BadRequest("Source video code is required", param).ReturnResponse();
            }

            var syncStartedAt = DateTime.Now;
            var count = await _service.SyncAsync(param);
            if (count == null)
            {
                _loger.LogError("Source video not found : " + param.SourceVideoCode);
                return new NotFound("Source video not found : " + param.SourceVideoCode, new { param.SourceVideoCode }).ReturnResponse();
            }

            // Reminder kandang ini yang dibuat sync ini langsung dikirim; yang gagal tetap Pending untuk ReminderSchedulerService
            try
            {
                await _reminderDeliveryService.SendPendingAsync(param.SourceVideoCode, syncStartedAt, HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _loger.LogError("Failed sending reminders after sync " + param.SourceVideoCode + " : " + ex.Message);
            }

            return new OK("Success sync data", new { count }).ReturnResponse();
        }

        [HttpPost("SyncStatus")]
        [AuthorizeFilter(Scope = "SucklingActivity.Add")]
        public async Task<IActionResult> SyncStatusAsync([FromBody] SucklingSyncStatusParam param)
        {
            if (param == null || string.IsNullOrWhiteSpace(param.SourceVideoCode))
            {
                _loger.LogError("Bad request parameter. Source video code is required");
                return new BadRequest("Source video code is required", param).ReturnResponse();
            }

            var isFound = await _sourceVideoStatusService.ReportAsync(param.SourceVideoCode, param.IsOnline, HttpContext.RequestAborted);
            if (!isFound)
            {
                _loger.LogError("Source video not found : " + param.SourceVideoCode);
                return new NotFound("Source video not found : " + param.SourceVideoCode, new { param.SourceVideoCode }).ReturnResponse();
            }

            return new OK("Success sync status", new { param.SourceVideoCode, param.IsOnline }).ReturnResponse();
        }

        [HttpPut]
        [AuthorizeFilter(Scope = "SucklingActivity.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] SucklingActivity data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");

                var error = new BadRequest("Parameter is null", data);
                return error.ReturnResponse();
            }

            var id = await _service.PutAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data");

                var error = new Conflict("Duplicate data", data);
                return error.ReturnResponse();
            }

            var result = new OK("Success update data", new { id });
            return result.ReturnResponse();
        }

        [HttpDelete("{id}")]
        [AuthorizeFilter(Scope = "SucklingActivity.Delete")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var data = await _service.GetAsync(id);
            if (data == null)
            {
                _loger.LogError("Data not found : " + id.ToString());

                var error = new NotFound("Data not found : " + id.ToString(), new { id });
                return error.ReturnResponse();
            }

            var isDeleted = await _service.DeleteAsync(id);
            if (!isDeleted)
            {
                var error = new NotFound("Failed deleted data", new { id });
                return error.ReturnResponse();
            }

            var result = new OK("Success delete data ", new { id });
            return result.ReturnResponse();
        }
    }
}
