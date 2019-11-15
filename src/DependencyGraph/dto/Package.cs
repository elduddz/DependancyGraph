using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyGraph
{
    public class Package
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string LicenseUrl { get; set; }
        public string DownloadUrl { get; internal set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
