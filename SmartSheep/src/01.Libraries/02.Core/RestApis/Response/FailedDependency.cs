using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class FailedDependency : RestApiResponse
    {
        public FailedDependency(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.FailedDependency, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.FailedDependency), message, data);
        }
    }
}
