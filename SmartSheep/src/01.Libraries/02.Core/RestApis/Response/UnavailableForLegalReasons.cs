using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class UnavailableForLegalReasons : RestApiResponse
    {
        public UnavailableForLegalReasons(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.UnavailableForLegalReasons, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.UnavailableForLegalReasons), message, data);
        }
    }
}
