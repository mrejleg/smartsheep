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
    public class AppMenuRoleController : BaseApiController
    {
        private readonly IAppMenuRoleService _service;
        private readonly IAppLogger<AppMenuRole> _loger;

        public AppMenuRoleController(IAppMenuRoleService service, IAppLogger<AppMenuRole> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All/{fkAppRoleId}")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
        public async Task<IActionResult> GetAllAsync(Guid fkAppRoleId)
        {
            var data = await _service.GetAll().Where(x => x.FkAppRoleId == fkAppRoleId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
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
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
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
        [AuthorizeFilter(Scope = "AppMenuRole.Add")]
        public async Task<IActionResult> PostAsync([FromBody] AppMenuRole data)
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
        [AuthorizeFilter(Scope = "AppMenuRole.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] AppMenuRole data)
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
        [AuthorizeFilter(Scope = "AppMenuRole.Delete")]
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

        [HttpGet("GetAllByAppRole/{fkAppRoleId}")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
        public async Task<IActionResult> GetAllByAppRole(Guid fkAppRoleId)
        {
            var data = await _service.GetAllByAppRole(fkAppRoleId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("GetAllByAppMenu/{fkAppMenuId}")]
        [AuthorizeFilter(Scope = "AppMenuRole.View")]
        public async Task<IActionResult> GetAllByAppMenu(Guid fkAppMenuId)
        {
            var data = await _service.GetAllByAppMenu(fkAppMenuId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpPost("Replace/{fkAppRoleId}")]
        [AuthorizeFilter(Scope = "AppMenuRole.Add")]
        public async Task<IActionResult> ReplaceAsync(Guid fkAppRoleId, [FromBody] List<AppMenuRole> data)
        {
            if (data == null)
            {
                return new BadRequest("Parameter is null", data).ReturnResponse();
            }

            var savedCount = await _service.ReplaceAsync(fkAppRoleId, data);
            if (!savedCount.HasValue)
            {
                return new BadRequest("Role or menu data is invalid", new { fkAppRoleId }).ReturnResponse();
            }

            return new OK("Success replace menu permissions", new { count = savedCount.Value }).ReturnResponse();
        }
    }
}
