using Newtonsoft.Json;

namespace DependencyGraph
{
    public class PackageVersion
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Version { get; set; }

        public Package[] Dependencies { get; set; }

        public License License { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}