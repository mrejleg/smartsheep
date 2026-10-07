using Api.ApplicationCore.Interfaces.Configs;
using Api.Controllers.FilterAttributes;
using Api.Domain.Entities.Configs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Constants;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Base.Models.JQueries.Select2s;
using Project.Core.Extentions;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Configs
{
    public class AppMenuController : BaseApiController
    {
        private readonly IAppMenuService _service;
        private readonly IAppLogger<AppMenu> _loger;

        public AppMenuController(IAppMenuService service, IAppLogger<AppMenu> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().Where(x => x.IsActive == true).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("GetAllByParent/{fkParentId}")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
        public async Task<IActionResult> GetAllByParent(Guid fkParentId)
        {
            var data = await _service.GetAllByParent(fkParentId).ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
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
        [AuthorizeFilter(Scope = "AppMenu.View")]
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
        [AuthorizeFilter(Scope = "AppMenu.Add")]
        public async Task<IActionResult> PostAsync([FromBody] AppMenu data)
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
        [AuthorizeFilter(Scope = "AppMenu.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] AppMenu data)
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
        [AuthorizeFilter(Scope = "AppMenu.Delete")]
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

        [HttpGet("Binding/DxDropDownBox/Parent")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
        public async Task<IActionResult> BindingDxDropDownBoxParentAsync()
        {
            var data = await _service.BindingDxDropDownBoxParentAsync();

            var result = new OK("Success binding DxDropDownBox", data);

            return result.ReturnResponse();
        }

        [HttpGet("Binding/Select2/AccessType")]
        [AuthorizeFilter(Scope = "AppMenu.View")]
        public IActionResult BindingSelect2AccessType()
        {
            var data = Enum.GetValues(typeof(AccessTypes))
                                .Cast<AccessTypes>()
                                .Select(x => new Select2Binding
                                {
                                    Id = EnumExtentions.GetEnumDescription(x),
                                    Text = x.ToString()
                                }).ToList();

            var result = new OK("Success binding select2", data);
            return result.ReturnResponse();
        }
    }
}
