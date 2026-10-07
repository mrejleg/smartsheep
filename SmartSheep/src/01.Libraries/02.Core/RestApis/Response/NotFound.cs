using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NotFound : RestApiResponse
    {
        public NotFound(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NotFound, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NotFound), message, data);
        }
    }
}
