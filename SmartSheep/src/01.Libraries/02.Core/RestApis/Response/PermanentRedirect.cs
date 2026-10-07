using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class PermanentRedirect : RestApiResponse
    {
        public PermanentRedirect(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.PermanentRedirect, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.PermanentRedirect), message, data);
        }
    }
}
