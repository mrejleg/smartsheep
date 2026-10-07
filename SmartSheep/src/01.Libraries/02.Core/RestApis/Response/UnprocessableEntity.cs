using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class UnprocessableEntity : RestApiResponse
    {
        public UnprocessableEntity(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.UnprocessableEntity, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.UnprocessableEntity), message, data);
        }
    }
}
