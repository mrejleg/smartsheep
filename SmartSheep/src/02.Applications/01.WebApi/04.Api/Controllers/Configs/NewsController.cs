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
    public class NewsController : BaseApiController
    {
        private readonly INewsService _service;
        private readonly IAppLogger<News> _loger;
        private readonly IWebHostEnvironment _environment;

        public NewsController(INewsService service, IAppLogger<News> loger, IWebHostEnvironment environment)
        {
            _service = service;
            _loger = loger;
            _environment = environment;
        }

        [HttpPost("UploadImage")]
        [AuthorizeFilter(Scope = "News.Add")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new BadRequest("Image file is required", null).ReturnResponse();
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };
            var extension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                return new BadRequest("Only JPG, JPEG, PNG, GIF, and WEBP images are allowed", null).ReturnResponse();
            }

            var directory = Path.Combine(_environment.WebRootPath, "uploads", "image");
            Directory.CreateDirectory(directory);
            var fileName = $"news-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var destination = Path.Combine(directory, fileName);
            await using (var stream = System.IO.File.Create(destination))
            {
                await file.CopyToAsync(stream);
            }

            return new OK("Success upload news image", new { Path = $"/uploads/image/{fileName}" }).ReturnResponse();
        }

        [HttpPost("DxGrid")]
        [AuthorizeFilter(Scope = "News.View")]
        public async Task<IActionResult> DxGridAsync([FromBody] DataSourceLoadOptions param)
        {
            var data = await _service.DxGridAsync(param);
            return new OK("success get all data", data).ReturnResponse();
        }

        [HttpGet("All")]
        [AuthorizeFilter(Scope = "News.View")]
        public async Task<IActionResult> GetAllAsync()
        {
            var data = await _service.GetAll().AsNoTracking().ToListAsync();
            return new OK("Success get all data", data).ReturnResponse();
        }

        [HttpGet("{id}")]
        [AuthorizeFilter(Scope = "News.View")]
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
        [AuthorizeFilter(Scope = "News.View")]
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
        [AuthorizeFilter(Scope = "News.Add")]
        public async Task<IActionResult> PostAsync([FromBody] News data)
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
                return new Conflict("Duplicate data", data).ReturnResponse();
            }

            return new OK("Success add data", new { id }).ReturnResponse();
        }

        [HttpPut]
        [AuthorizeFilter(Scope = "News.Edit")]
        public async Task<IActionResult> PutAsync([FromBody] News data)
        {
            if (data == null)
            {
                _loger.LogError("Bad request parameter. Parameter is null");
                return new BadRequest("Parameter is null", data).ReturnResponse();
            }

            var id = await _service.PutAsync(data);
            if (id == null)
            {
                _loger.LogError("Duplicate data");
                return new Conflict("Duplicate data", data).ReturnResponse();
            }

            return new OK("Success update data", new { id }).ReturnResponse();
        }

        [HttpDelete("{id}")]
        [AuthorizeFilter(Scope = "News.Delete")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var data = await _service.GetAsync(id);
            if (data == null)
            {
                _loger.LogError("Data not found : " + id);
                return new NotFound("Data not found : " + id, new { id }).ReturnResponse();
            }

            var isDeleted = await _service.DeleteAsync(id);
            if (!isDeleted)
            {
                return new NotFound("Failed deleted data", new { id }).ReturnResponse();
            }

            return new OK("Success delete data ", new { id }).ReturnResponse();
        }
    }
}
