using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Ambiguous : RestApiResponse
    {
        public Ambiguous(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Ambiguous, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Ambiguous), message, data);
        }
    }
}
