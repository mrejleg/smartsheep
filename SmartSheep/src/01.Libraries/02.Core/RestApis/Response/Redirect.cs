using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Redirect : RestApiResponse
    {
        public Redirect(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Redirect, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Redirect), message, data);
        }
    }
}
