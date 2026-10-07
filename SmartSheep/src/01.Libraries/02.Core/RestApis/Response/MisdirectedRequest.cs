using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class MisdirectedRequest : RestApiResponse
    {
        public MisdirectedRequest(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.MisdirectedRequest, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.MisdirectedRequest), message, data);
        }
    }
}
