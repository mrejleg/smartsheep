using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class TooManyRequests : RestApiResponse
    {
        public TooManyRequests(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.TooManyRequests, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.TooManyRequests), message, data);
        }
    }
}
