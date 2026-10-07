using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Locked : RestApiResponse
    {
        public Locked(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Locked, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Locked), message, data);
        }
    }
}
