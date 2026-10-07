using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class MultipleChoices : RestApiResponse
    {
        public MultipleChoices(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.MultipleChoices, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.MultipleChoices), message, data);
        }
    }
}
