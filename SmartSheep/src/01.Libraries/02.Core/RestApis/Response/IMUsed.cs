using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class IMUsed : RestApiResponse
    {
        public IMUsed(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.IMUsed, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.IMUsed), message, data);
        }
    }
}
