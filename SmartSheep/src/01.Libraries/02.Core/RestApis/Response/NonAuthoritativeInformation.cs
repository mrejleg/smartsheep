using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NonAuthoritativeInformation : RestApiResponse
    {
        public NonAuthoritativeInformation(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NonAuthoritativeInformation, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NonAuthoritativeInformation), message, data);
        }
    }
}
