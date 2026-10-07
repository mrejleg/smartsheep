using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class PartialContent : RestApiResponse
    {
        public PartialContent(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.PartialContent, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.PartialContent), message, data);
        }
    }
}
