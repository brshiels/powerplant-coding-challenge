using System.Text.Json.Serialization;

namespace powerplant.Dtos
{
    public class Reply
    {
        public string Name { get; set; }
        public decimal P { get; set; }
        [JsonIgnore]
        public decimal Cost { get; set; }
    }
}