using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Found : RestApiResponse
    {
        public Found(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Found, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Found), message, data);
        }
    }
}
