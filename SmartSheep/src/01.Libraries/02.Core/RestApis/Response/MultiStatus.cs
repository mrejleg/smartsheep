using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class MultiStatus : RestApiResponse
    {
        public MultiStatus(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.MultiStatus, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.MultiStatus), message, data);
        }
    }
}
