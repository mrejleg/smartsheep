using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Gone : RestApiResponse
    {
        public Gone(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Gone, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Gone), message, data);
        }
    }
}
