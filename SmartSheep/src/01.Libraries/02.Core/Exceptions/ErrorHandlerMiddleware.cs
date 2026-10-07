using Microsoft.AspNetCore.Http;
using Project.Core.RestApis.Response;
using System.Text.Json;

namespace Project.Core.Exceptions
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new InternalServerError("Internal server eror from middleware - " + ex.Message, null));

                await response.WriteAsync(result);
            }
        }
    }
}
