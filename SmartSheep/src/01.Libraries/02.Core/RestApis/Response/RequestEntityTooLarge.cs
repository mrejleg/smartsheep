using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RequestEntityTooLarge : RestApiResponse
    {
        public RequestEntityTooLarge(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RequestEntityTooLarge, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RequestEntityTooLarge), message, data);
        }
    }
}
