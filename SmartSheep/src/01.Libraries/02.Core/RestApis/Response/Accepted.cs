using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Accepted : RestApiResponse
    {
        public Accepted(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Accepted, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Accepted), message, data);
        }
    }
}
