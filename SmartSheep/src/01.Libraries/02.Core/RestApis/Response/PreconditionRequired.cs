using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class PreconditionRequired : RestApiResponse
    {
        public PreconditionRequired(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.PreconditionRequired, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.PreconditionRequired), message, data);
        }
    }
}
