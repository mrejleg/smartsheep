using Newtonsoft.Json;

namespace Project.Base.Models.Firebases
{
    public class NotificationRequestDto
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("username")]
        public string[] Username { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        public string FirebaseUrl { get; set; }
    }
}
