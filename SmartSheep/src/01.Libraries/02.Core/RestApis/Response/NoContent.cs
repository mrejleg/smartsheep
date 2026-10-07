using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NoContent : RestApiResponse
    {
        public NoContent(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NoContent, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NoContent), message, data);
        }
    }
}
