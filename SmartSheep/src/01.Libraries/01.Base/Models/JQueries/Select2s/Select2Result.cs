using System.Text.Json.Serialization;

namespace Project.Base.Models.JQueries.Select2s
{
    public class Select2Result
    {
        [JsonPropertyName("result")]
        public IReadOnlyList<Select2Binding> Result { get; set; }

        [JsonPropertyName("key")]
        public object Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
