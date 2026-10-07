using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class UseProxy : RestApiResponse
    {
        public UseProxy(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.UseProxy, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.UseProxy), message, data);
        }
    }
}
