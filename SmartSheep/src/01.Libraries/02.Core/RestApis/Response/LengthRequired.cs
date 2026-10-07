using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class LengthRequired : RestApiResponse
    {
        public LengthRequired(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.LengthRequired, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.LengthRequired), message, data);
        }
    }
}
