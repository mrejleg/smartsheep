using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Project.Core.Toasts.Helpers
{
    public static class JsonSerialization
    {
        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        }
    }
}
