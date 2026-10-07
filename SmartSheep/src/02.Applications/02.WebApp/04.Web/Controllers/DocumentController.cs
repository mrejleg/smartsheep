using Microsoft.AspNetCore.Mvc;
using Project.Core.HttpClients;
using Project.Core.Toasts.Abstractions;
using System.Net;
using Web.Domain.Models;
using Web.Infrastructure;

namespace Web.Controllers
{
    public class DocumentController : BaseWebController
    {
        public DocumentController(INotyfService notyfService, AppSetting appSetting) : base(notyfService, appSetting)
        {
        }

        public async Task<ActionResult> DisplayImageAsync(string fileName)
        {
            var client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSetting));
            var response = await HttpClientExtentions.SecuredGetFileAsync(client, "Document/display/image?fileName=" + fileName);

            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                var memory = new MemoryStream();

                var fileStream = (Stream)response.Data;
                fileStream.Seek(0, SeekOrigin.Begin);
                await fileStream.CopyToAsync(memory);

                memory.Position = 0;
                return File(memory, response.FileType, response.FileName);
            }

            return null;
        }

        public async Task<ActionResult> DownloadFileAsync(string fileName)
        {
            var client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSetting));
            var response = await HttpClientExtentions.SecuredGetFileAsync(client, "Document/download?fileName=" + fileName);

            if (response.StatusCode == (int)HttpStatusCode.OK)
            {
                var memory = new MemoryStream();

                var fileStream = (Stream)response.Data;
                fileStream.Seek(0, SeekOrigin.Begin);
                await fileStream.CopyToAsync(memory);

                memory.Position = 0;
                return File(memory, response.FileType, response.FileName);
            }

            return null;
        }
    }
}
