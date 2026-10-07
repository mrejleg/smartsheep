using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NotModified : RestApiResponse
    {
        public NotModified(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NotModified, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NotModified), message, data);
        }
    }
}
