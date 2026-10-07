using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class ExpectationFailed : RestApiResponse
    {
        public ExpectationFailed(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.ExpectationFailed, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.ExpectationFailed), message, data);
        }
    }
}
