using Newtonsoft.Json;
using Project.Base.Models.Notifications;
using System.Net.Mime;
using System.Text;

namespace Project.Core.Emails
{
    public static class BrevoEmailExtention
    {
        public static async Task<bool> SendEmailAsync(SendEmailParam param)
        {
            var result = false;
            if (bool.Parse(param.IsUseEmail))
            {
                using var client = ClientBearear(param);
                using var response = await client.PostAsync(client.BaseAddress.ToString() + "Email/SendEmail", new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    result = true;
                }
            }

            return result;
        }

        public static HttpClient ClientBearear(SendEmailParam param)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(param.Url)
            };

            return client;
        }
    }
}
