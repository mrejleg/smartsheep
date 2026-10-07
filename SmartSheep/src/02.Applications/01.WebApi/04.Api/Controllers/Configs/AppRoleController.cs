using Api.ApplicationCore.Interfaces.Configs;
using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.Configs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Configs
{
    public class AppRoleController : BaseApiController
    {
        private readonly IAppRoleService _service;
        private readonly IAppLogger<AppRole> _loger;

        public AppRoleController(IAppRoleService service, IAppLogger<AppRole> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "AppRole.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "AppRole.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "AppRole.View")]
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

        [HttpGet("Name/{name}")]
        [AuthorizeFilter(Scope = "AppRole.View")]
        public async Task<IActionResult> GetAsync(string name)
        {
            var data = await _service.GetByNameAsync(name);

            if (data == null)
            {
                _loger.LogError("Data not found : " + name);

                var error = new NotFound("Data not found : " + name, new { name });
                return error.ReturnResponse();
            }

            var result = new OK("Success get data by " + name, data);
            return result.ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "AppRole.View")]
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
        [AuthorizeFilter(Scope = "AppRole.Add")]
        public async Task<IActionResult> PostAsync([FromBody] AppRole data)
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
                _loger.LogError("Duplicate data : " + data.Name);

                var error = new Conflict("Duplicate data : " + data.Name, data);
                return error.ReturnResponse();
            }

            var result = new OK("Success add data", new { id });
            return result.ReturnResponse();
        }

        [HttpPut]
        [AuthorizeFilter(Scope = "AppRole.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] AppRole data)
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
                _loger.LogError("Duplicate data : " + data.Name);

                var error = new Conflict("Duplicate data : " + data.Name, data);
                return error.ReturnResponse();
            }

            var result = new OK("Success update data", new { id });
            return result.ReturnResponse();
        }

        [HttpDelete("{id}")]
        [AuthorizeFilter(Scope = "AppRole.Delete")]
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

        [HttpGet("Binding/DxDropDownBox")]
        [AuthorizeFilter(Scope = "AppRole.View")]
        public async Task<IActionResult> BindingDxDropDownBoxAsync()
        {
            var data = await _service.BindingDxDropDownBoxAsync();

            var result = new OK("Success binding DxDropDownBox", data);

            return result.ReturnResponse();
        }
    }
}
