using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Processing : RestApiResponse
    {
        public Processing(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Processing, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Processing), message, data);
        }
    }
}
