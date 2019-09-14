using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DependancyGraph
{
    public class Package
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        public string Name { get; set; }
        public PackageVersion[] Versions { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
