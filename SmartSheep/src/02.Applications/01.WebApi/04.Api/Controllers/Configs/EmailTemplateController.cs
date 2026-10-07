using Api.ApplicationCore.Interfaces.Configs;
using Api.Domain.Entities.Configs;
using Api.Controllers.FilterAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.Interfaces.Logging;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.Configs
{
    public class EmailTemplateController : BaseApiController
    {
        private readonly IEmailTemplateService _service;
        private readonly IAppLogger<EmailTemplate> _loger;

        public EmailTemplateController(IEmailTemplateService service, IAppLogger<EmailTemplate> loger)
        {
            _service = service;
            _loger = loger;
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            var result = new OK("success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().ToListAsync();
            var result = new OK("Success get all data", data);
            return result.ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
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

        [HttpGet("Code/{key}")]
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
        public async Task<IActionResult> GetAsync(string key)
        {
            var data = await _service.GetAsync(key);

            if (data == null)
            {
                _loger.LogError("Data not found : " + key.ToString());

                var error = new NotFound("Data not found : " + key.ToString(), data);
                return error.ReturnResponse();
            }

            var result = new OK("Success get data by " + key.ToString(), data);
            return result.ReturnResponse();
        }

        [HttpPost("Param")]
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
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
        [AuthorizeFilter(Scope = "EmailTemplate.Add")]
        public async Task<IActionResult> PostAsync([FromBody] EmailTemplate data)
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
        [AuthorizeFilter(Scope = "EmailTemplate.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] EmailTemplate data)
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
        [AuthorizeFilter(Scope = "EmailTemplate.Delete")]
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
        [AuthorizeFilter(Scope = "EmailTemplate.View")]
        public async Task<IActionResult> BindingDxDropDownBoxAsync()
        {
            var data = await _service.BindingDxDropDownBoxAsync();

            var result = new OK("Success binding DxDropDownBox", data);

            return result.ReturnResponse();
        }
    }
}
