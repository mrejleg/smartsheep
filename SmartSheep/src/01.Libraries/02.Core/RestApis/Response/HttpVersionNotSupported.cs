using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class HttpVersionNotSupported : RestApiResponse
    {
        public HttpVersionNotSupported(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.HttpVersionNotSupported, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.HttpVersionNotSupported), message, data);
        }
    }
}
