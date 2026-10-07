using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class VariantAlsoNegotiates : RestApiResponse
    {
        public VariantAlsoNegotiates(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.VariantAlsoNegotiates, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.VariantAlsoNegotiates), message, data);
        }
    }
}
