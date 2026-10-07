using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Unauthorized : RestApiResponse
    {
        public Unauthorized(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Unauthorized, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Unauthorized), message, data);
        }
    }
}
