using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RedirectKeepVerb : RestApiResponse
    {
        public RedirectKeepVerb(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RedirectKeepVerb, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RedirectKeepVerb), message, data);
        }
    }
}
