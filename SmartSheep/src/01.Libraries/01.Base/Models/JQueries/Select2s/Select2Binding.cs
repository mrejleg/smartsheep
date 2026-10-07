using System.Text.Json.Serialization;

namespace Project.Base.Models.JQueries.Select2s
{
    public class Select2Binding
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
