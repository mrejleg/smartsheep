using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class BadGateway : RestApiResponse
    {
        public BadGateway(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.BadGateway, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.BadGateway), message, data);
        }
    }
}
