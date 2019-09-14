using Newtonsoft.Json;

namespace DependancyGraph
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