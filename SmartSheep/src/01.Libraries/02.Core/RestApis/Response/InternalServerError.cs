using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class InternalServerError : RestApiResponse
    {
        public InternalServerError(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.InternalServerError, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.InternalServerError), message, data);
        }
    }
}
