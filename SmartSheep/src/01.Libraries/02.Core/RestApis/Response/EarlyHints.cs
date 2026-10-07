using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class EarlyHints : RestApiResponse
    {
        public EarlyHints(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.EarlyHints, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.EarlyHints), message, data);
        }
    }
}
