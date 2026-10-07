using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class InsufficientStorage : RestApiResponse
    {
        public InsufficientStorage(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.InsufficientStorage, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.InsufficientStorage), message, data);
        }
    }
}
