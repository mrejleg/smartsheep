using Api.ApplicationCore.Interfaces.Auths;
using Api.Controllers;
using Api.Controllers.FilterAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;
using Api.Domain.ApiManagements;
using Api.ApplicationCore.Interfaces.ApiManagements;

namespace Api.Controllers.ApiManagements
{
    public class AppClientController : BaseApiController
    {
        private readonly IAppClientService _service;
        private readonly IAuthApiService _authService;
        private readonly IAppLogger<AppClient> _loger;

        public AppClientController(IAppClientService service, IAuthApiService authService, IAppLogger<AppClient> loger)
        {
            _service = service;
            _authService = authService;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "AppClient.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "AppClient.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAllAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("GetAll")]
        [AuthorizeFilter(Scope = "AppClient.View")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "AppClient.View")]
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

        [HttpGet("Client/{clientId}")]
        [AuthorizeFilter(Scope = "AppClient.View")]
        public async Task<IActionResult> GetAsync(string clientId)
        {
            var data = await _service.GetAsync(clientId);

            if (data == null)
            {
                _loger.LogError("Data not found : " + clientId.ToString());

                var error = new NotFound("Data not found : " + clientId.ToString(), new { clientId });
                return error.ReturnResponse();
            }

            var result = new OK("Success get data by " + clientId.ToString(), data);
            return result.ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "AppClient.View")]
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
        [AuthorizeFilter(Scope = "AppClient.Add")]
        public async Task<IActionResult> PostAsync([FromBody] AppClient data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");

                var error = new BadRequest("Parameter is null", data);
                return error.ReturnResponse();
            }

            data.ClientSecret = _authService.GetClientSecret(data.ClientId);
            var id = await _service.PostAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data : " + data.Name);

                var error = new Conflict("Duplicate data : " + data.Name, data);
                return error.ReturnResponse();
            }

            var result = new OK("Success add data", id);
            return result.ReturnResponse();
        }

        [HttpPut]
        [AuthorizeFilter(Scope = "AppClient.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] AppClient data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");

                var error = new BadRequest("Parameter is null", data);
                return error.ReturnResponse();
            }

            data.ClientSecret = _authService.GetClientSecret(data.ClientId);
            var id = await _service.PutAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data : " + data.Name);
                var error = new Conflict("Duplicate data : " + data.Name, data);
                return error.ReturnResponse();
            }

            var result = new OK("Success update data", new { id });
            return result.ReturnResponse();
        }

        [HttpDelete("{id}")]
        [AuthorizeFilter(Scope = "AppClient.Delete")]
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

        [HttpGet("Binding/DxDropDownBox")]
        [AuthorizeFilter(Scope = "AppClient.View")]
        public async Task<IActionResult> BindingDxDropDownBoxAsync()
        {
            var data = await _service.BindingDxDropDownBoxAsync();

            var result = new OK("Success binding DxDropDownBox", data);

            return result.ReturnResponse();
        }
    }
}
