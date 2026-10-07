using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class MovedPermanently : RestApiResponse
    {
        public MovedPermanently(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.MovedPermanently, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.MovedPermanently), message, data);
        }
    }
}
