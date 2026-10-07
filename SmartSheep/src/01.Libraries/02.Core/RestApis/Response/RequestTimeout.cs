using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RequestTimeout : RestApiResponse
    {
        public RequestTimeout(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RequestTimeout, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RequestTimeout), message, data);
        }
    }
}
