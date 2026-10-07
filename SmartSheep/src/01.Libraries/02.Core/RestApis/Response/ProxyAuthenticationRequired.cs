using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class ProxyAuthenticationRequired : RestApiResponse
    {
        public ProxyAuthenticationRequired(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.ProxyAuthenticationRequired, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.ProxyAuthenticationRequired), message, data);
        }
    }
}
