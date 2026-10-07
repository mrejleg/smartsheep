using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class UnsupportedMediaType : RestApiResponse
    {
        public UnsupportedMediaType(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.UnsupportedMediaType, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.UnsupportedMediaType), message, data);
        }
    }
}
