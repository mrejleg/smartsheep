using System.Text.Json.Serialization;

namespace Project.Base.Models.DevExpress.DxDropDownBoxs
{
    public class DxDropDownBoxBinding
    {
        [JsonPropertyName("Id")]
        public object Id { get; set; }

        [JsonPropertyName("Text")]
        public string Text { get; set; }
    }
}
