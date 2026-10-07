using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class MethodNotAllowed : RestApiResponse
    {
        public MethodNotAllowed(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.MethodNotAllowed, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.MethodNotAllowed), message, data);
        }
    }
}
