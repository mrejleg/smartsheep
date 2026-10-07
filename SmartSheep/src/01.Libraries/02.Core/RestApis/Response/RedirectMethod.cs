using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class RedirectMethod : RestApiResponse
    {
        public RedirectMethod(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.RedirectMethod, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.RedirectMethod), message, data);
        }
    }
}
