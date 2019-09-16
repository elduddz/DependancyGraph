using Newtonsoft.Json;

namespace DependencyGraph
{
    public class LicenseType
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Name{ get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}