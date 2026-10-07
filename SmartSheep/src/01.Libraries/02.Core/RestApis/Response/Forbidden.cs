using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Forbidden : RestApiResponse
    {
        public Forbidden(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Forbidden, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Forbidden), message, data);
        }
    }
}
