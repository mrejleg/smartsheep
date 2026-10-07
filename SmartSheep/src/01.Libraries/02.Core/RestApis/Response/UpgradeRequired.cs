using Project.Base.Models.RestApis;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class UpgradeRequired : RestApiResponse
    {
        public UpgradeRequired(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.UpgradeRequired, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.UpgradeRequired), message, data);
        }
    }
}
