using Microsoft.AspNetCore.Http;
using Project.Core.RestApis.Response;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Project.Core.Exceptions.AntiXssMiddlewares
{
    public class AntiXssMiddleware
    {
        private readonly RequestDelegate _next;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            // Check XSS in URL
            if (!string.IsNullOrWhiteSpace(context.Request.Path.Value))
            {
                var url = context.Request.Path.Value;

                if (CrossSiteScriptingValidation.IsDangerousString(url, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }
            }

            // Check XSS in query string
            if (!string.IsNullOrWhiteSpace(context.Request.QueryString.Value))
            {
                var queryString = WebUtility.UrlDecode(context.Request.QueryString.Value);

                if (CrossSiteScriptingValidation.IsDangerousString(queryString, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }
            }

            // Check XSS in request content
            var originalBody = context.Request.Body;

            try
            {
                var content = await ReadRequestBody(context);

                if (CrossSiteScriptingValidation.IsSafeFile(content))
                {
                    await _next(context).ConfigureAwait(false);
                    return;
                }

                if (CrossSiteScriptingValidation.IsDangerousString(content, out _))
                {
                    await RespondWithAnError(context).ConfigureAwait(false);
                    return;
                }

                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                context.Request.Body = originalBody;
            }
        }

        private static async Task<string> ReadRequestBody(HttpContext context)
        {
            var buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer);
            context.Request.Body = buffer;
            buffer.Position = 0;

            var encoding = Encoding.UTF8;

            var requestContent = await new StreamReader(buffer, encoding).ReadToEndAsync();
            context.Request.Body.Position = 0;

            return requestContent;
        }

        private static async Task RespondWithAnError(HttpContext context)
        {
            var response = context.Response;
            response.Headers.Clear();
            response.Headers.AddHeaders();
            response.ContentType = "application/json; charset=utf-8";

            var result = JsonSerializer.Serialize(new InternalServerError($"XSS injection detected from middleware.", null));
            await response.WriteAsync(result);
        }
    }
}
