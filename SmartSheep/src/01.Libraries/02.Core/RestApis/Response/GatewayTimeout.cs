using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class GatewayTimeout : RestApiResponse
    {
        public GatewayTimeout(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.GatewayTimeout, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.GatewayTimeout), message, data);
        }
    }
}
