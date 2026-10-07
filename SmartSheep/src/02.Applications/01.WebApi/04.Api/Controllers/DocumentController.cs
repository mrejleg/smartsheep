using Api.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Project.Core.Files;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers
{
    public class DocumentController : BaseApiController
    {
        private readonly AppSetting _appSetting;
        public DocumentController(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadAsync([FromQuery] string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);
            if (!string.IsNullOrEmpty(safeFileName) && safeFileName == fileName && IsAllowExtention(safeFileName))
            {
                var fileSource = Path.Combine(_appSetting.FileUrl, "files", "uploads", safeFileName);
                if (!System.IO.File.Exists(fileSource))
                {
                    return new NotFound("File not found", safeFileName).ReturnResponse();
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(fileSource);
                var mimeType = Path.GetExtension(fileSource).GetMimeType();
                return File(bytes, mimeType, safeFileName);
            }

            var result = new BadRequest("File not support", fileName);
            return result.ReturnResponse();
        }

        private bool IsAllowExtention(string fileName)
        {
            var result = false;
            string extension = Path.GetExtension(fileName);

            if (extension.ToLower() == ".bmp" ||
                extension.ToLower() == ".gif" ||
                extension.ToLower() == ".ico" ||
                extension.ToLower() == ".jpg" ||
                extension.ToLower() == ".jpeg" ||
                extension.ToLower() == ".png" ||
                extension.ToLower() == ".tif" ||
                extension.ToLower() == ".tiff" ||
                extension.ToLower() == ".wmf" ||
                extension.ToLower() == ".xlsx" ||
                extension.ToLower() == ".xls" ||
                extension.ToLower() == ".docx" ||
                extension.ToLower() == ".doc" ||
                extension.ToLower() == ".pptx" ||
                extension.ToLower() == ".ppt" ||
                extension.ToLower() == ".pdf" ||
                extension.ToLower() == ".csv" ||
                extension.ToLower() == ".zip" ||
                extension.ToLower() == ".rar"
                )
            {
                result = true;
            }

            return result;
        }
    }
}
