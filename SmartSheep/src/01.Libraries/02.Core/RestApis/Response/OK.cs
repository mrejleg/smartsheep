using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class OK : RestApiResponse
    {
        public OK(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.OK, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.OK), message, data);
        }
    }
}
