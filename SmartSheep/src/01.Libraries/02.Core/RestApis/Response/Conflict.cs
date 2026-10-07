using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Conflict : RestApiResponse
    {
        public Conflict(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Conflict, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Conflict), message, data);
        }
    }
}
