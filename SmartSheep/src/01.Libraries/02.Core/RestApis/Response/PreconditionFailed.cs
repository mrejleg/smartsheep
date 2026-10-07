using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class PreconditionFailed : RestApiResponse
    {
        public PreconditionFailed(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.PreconditionFailed, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.PreconditionFailed), message, data);
        }
    }
}
