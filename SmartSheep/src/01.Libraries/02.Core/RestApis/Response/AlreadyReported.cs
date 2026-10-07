using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class AlreadyReported : RestApiResponse
    {
        public AlreadyReported(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.AlreadyReported, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.AlreadyReported), message, data);
        }
    }
}
