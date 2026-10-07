using Project.Base.Models.RestApis;
using System;
using System.Net;

namespace Project.Core.RestApis.Response
{
    public class Created : RestApiResponse
    {
        public Created(string message, object data)
        {
            SetRestApiResponse((int)HttpStatusCode.Created, Enum.GetName(typeof(HttpStatusCode), (int)HttpStatusCode.Created), message, data);
        }
    }
}
