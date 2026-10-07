using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Unused : RestApiResponse
    {
        public Unused(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Unused, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Unused), message, data);
        }
    }
}
