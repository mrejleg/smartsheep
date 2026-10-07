using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RequestedRangeNotSatisfiable : RestApiResponse
    {
        public RequestedRangeNotSatisfiable(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RequestedRangeNotSatisfiable, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RequestedRangeNotSatisfiable), message, data);
        }
    }
}
