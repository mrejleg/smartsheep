using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class SwitchingProtocols : RestApiResponse
    {
        public SwitchingProtocols(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.SwitchingProtocols, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.SwitchingProtocols), message, data);
        }
    }
}
