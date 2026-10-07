using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class PaymentRequired : RestApiResponse
    {
        public PaymentRequired(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.PaymentRequired, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.PaymentRequired), message, data);
        }
    }
}
