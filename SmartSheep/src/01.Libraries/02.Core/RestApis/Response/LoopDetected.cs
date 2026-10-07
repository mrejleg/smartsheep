using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class LoopDetected : RestApiResponse
    {
        public LoopDetected(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.LoopDetected, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.LoopDetected), message, data);
        }
    }
}
