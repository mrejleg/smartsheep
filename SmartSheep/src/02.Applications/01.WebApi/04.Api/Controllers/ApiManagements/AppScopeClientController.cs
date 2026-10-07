using Api.ApplicationCore.Interfaces.ApiManagements;
using Api.Controllers;
using Api.Controllers.FilterAttributes;
using Api.Domain.ApiManagements;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.ApiManagements
{
    public class AppScopeClientController : BaseApiController
    {
        private readonly IAppScopeClientService _service;
        private readonly IAppLogger<AppScopeClient> _loger;

        public AppScopeClientController(IAppScopeClientService service, IAppLogger<AppScopeClient> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All/{fkAppClientId}")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAllAsync(Guid fkAppClientId)
        {
            var data = await _service.GetAll(fkAppClientId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var data = await _service.GetAsync(id);

            if (data == null)
            {
                _loger.LogError("Data not found : " + id.ToString());

                var error = new NotFound("Data not found : " + id.ToString(), new { id });
                return error.ReturnResponse();
            }

            var result = new OK("Success get data by " + id.ToString(), data);
            return result.ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAsync([FromBody] DataParameter param)
        {
            if (param.Start <= 0 || param.Length <= 0)
            {
                _loger.LogError("Bad request parameter. Start page > 0 and page size > 0");

                var error = new BadRequest("Start page > 0 and page size > 0", new { param.Start, param.Length });
                return error.ReturnResponse();
            }

            var data = await _service.GetAsync(param);
            var result = new OK("Success get data by paging", data);
            return result.ReturnResponse();
        }

        [HttpPost]
        [AuthorizeFilter(Scope = "AppScopeClient.Add")]
        public async Task<IActionResult> PostAsync([FromBody] AppScopeClient data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");

                var error = new BadRequest("Parameter is null", data);
                return error.ReturnResponse();
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
        [AuthorizeFilter(Scope = "AppScopeClient.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] AppScopeClient data)
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
        [AuthorizeFilter(Scope = "AppScopeClient.Delete")]
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
                var error = new NotFound("Failed to deleted data", new { id });
                return error.ReturnResponse();
            }

            var result = new OK("Success delete data ", new { id });
            return result.ReturnResponse();
        }

        [HttpGet("GetAllByAppClient/{fkAppClientId}")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAllByAppClient(Guid fkAppClientId)
        {
            var data = await _service.GetAllByAppClient(fkAppClientId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("GetAllByScopeClient/{fkScopeClientId}")]
        [AuthorizeFilter(Scope = "AppScopeClient.View")]
        public async Task<IActionResult> GetAllByScopeClient(Guid fkScopeClientId)
        {
            var data = await _service.GetAllByScopeClient(fkScopeClientId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpPost("Replace/{fkAppClientId}")]
        [AuthorizeFilter(Scope = "AppScopeClient.Add")]
        public async Task<IActionResult> ReplaceAsync(Guid fkAppClientId, [FromBody] List<AppScopeClient> data)
        {
            if (data == null)
            {
                return new BadRequest("Parameter is null", data).ReturnResponse();
            }

            var savedCount = await _service.ReplaceAsync(fkAppClientId, data);
            if (!savedCount.HasValue)
            {
                return new BadRequest("Application client or scope data is invalid", new { fkAppClientId }).ReturnResponse();
            }

            return new OK("Success replace application scopes", new { count = savedCount.Value }).ReturnResponse();
        }
    }
}
