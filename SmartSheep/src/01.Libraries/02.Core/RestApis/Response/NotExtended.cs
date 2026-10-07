using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class NotExtended : RestApiResponse
    {
        public NotExtended(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.NotExtended, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.NotExtended), message, data);
        }
    }
}
