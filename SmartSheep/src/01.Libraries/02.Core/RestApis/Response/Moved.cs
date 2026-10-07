using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Moved : RestApiResponse
    {
        public Moved(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Moved, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Moved), message, data);
        }
    }
}
