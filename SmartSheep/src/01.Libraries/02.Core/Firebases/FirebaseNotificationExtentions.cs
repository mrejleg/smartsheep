using Project.Base.Models.Firebases;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Project.Core.Firebases
{
    public static class FirebaseNotificationExtentions
    {
        public static async Task<bool> SendNotificationAsync(IHttpClientFactory httpContextFactory, NotificationRequestDto param)
        {
            var client = httpContextFactory.CreateClient();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var payload = JsonSerializer.Serialize(param, options);
            var payloads = new StringContent(payload, Encoding.UTF8, MediaTypeNames.Application.Json);
            var response = await client.PostAsync(param.FirebaseUrl, payloads);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }
    }
}
