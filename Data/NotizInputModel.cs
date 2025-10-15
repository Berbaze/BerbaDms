using Newtonsoft.Json;

namespace DmsProjeckt.Data
{
    public class NotizInputModel
    {
        public int? Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}
