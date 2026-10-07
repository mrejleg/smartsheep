using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class ResetContent : RestApiResponse
    {
        public ResetContent(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.ResetContent, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.ResetContent), message, data);
        }
    }
}
