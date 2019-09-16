using Newtonsoft.Json;

namespace DependencyGraph
{
    public class License
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string uri { get; set; }

        public LicenseType LicenseType { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}