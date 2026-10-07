using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class ServiceUnavailable : RestApiResponse
    {
        public ServiceUnavailable(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.ServiceUnavailable, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.ServiceUnavailable), message, data);
        }
    }
}
