using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class TemporaryRedirect : RestApiResponse
    {
        public TemporaryRedirect(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.TemporaryRedirect, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.TemporaryRedirect), message, data);
        }
    }
}
