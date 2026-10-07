using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Continue : RestApiResponse
    {
        public Continue(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Continue, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Continue), message, data);
        }
    }
}
