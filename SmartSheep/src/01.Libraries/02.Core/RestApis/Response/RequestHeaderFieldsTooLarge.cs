using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RequestHeaderFieldsTooLarge : RestApiResponse
    {
        public RequestHeaderFieldsTooLarge(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RequestHeaderFieldsTooLarge, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RequestHeaderFieldsTooLarge), message, data);
        }
    }
}
