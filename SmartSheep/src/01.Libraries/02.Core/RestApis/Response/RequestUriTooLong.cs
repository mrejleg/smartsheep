using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RequestUriTooLong : RestApiResponse
    {
        public RequestUriTooLong(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RequestUriTooLong, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RequestUriTooLong), message, data);
        }
    }
}
