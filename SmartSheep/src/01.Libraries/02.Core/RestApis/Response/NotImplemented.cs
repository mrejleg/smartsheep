using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NotImplemented : RestApiResponse
    {
        public NotImplemented(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NotImplemented, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NotImplemented), message, data);
        }
    }
}
