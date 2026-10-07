using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.SmartSheep;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.SmartSheep
{
    public class JobExecutionLogController : BaseApiController
    {
        private readonly IJobExecutionLogService _service;
        private readonly IAppLogger<JobExecutionLog> _loger;

        public JobExecutionLogController(IJobExecutionLogService service, IAppLogger<JobExecutionLog> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "JobExecutionLog.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            return new OK("success get all data", data).ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "JobExecutionLog.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            return new OK("Success get all data", data).ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "JobExecutionLog.View")]
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
        [AuthorizeFilter(Scope = "JobExecutionLog.View")]
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
        [AuthorizeFilter(Scope = "JobExecutionLog.Add")]
        public async Task<IActionResult> PostAsync([FromBody] JobExecutionLog data)
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

        [HttpPut]
        [AuthorizeFilter(Scope = "JobExecutionLog.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] JobExecutionLog data)
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
        [AuthorizeFilter(Scope = "JobExecutionLog.Delete")]
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
