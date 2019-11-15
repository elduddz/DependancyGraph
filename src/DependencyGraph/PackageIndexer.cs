using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using System.Linq;
using Gremlin.Net.CosmosDb;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Structure;
using static Gremlin.Net.Process.Traversal.AnonymousTraversalSource;
using static Gremlin.Net.Process.Traversal.__;
using static Gremlin.Net.Process.Traversal.P;
using static Gremlin.Net.Process.Traversal.Order;
using static Gremlin.Net.Process.Traversal.Operator;
using static Gremlin.Net.Process.Traversal.Pop;
using static Gremlin.Net.Process.Traversal.Scope;
using static Gremlin.Net.Process.Traversal.TextP;
using static Gremlin.Net.Process.Traversal.Column;
using static Gremlin.Net.Process.Traversal.Direction;
using static Gremlin.Net.Process.Traversal.T;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Text.RegularExpressions;

namespace DependencyGraph
{
    public static class PackageIndexer
    {
        private static Container container;
        private static ILogger _log;
        private static IndexerService _indexerService;

        [FunctionName("PackageIndexer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var indexer = new IndexerService(log);
            _log = log;
            _log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string version = req.Query["version"];
            string frameworkFilter = req.Query["framework"];
            string license = req.Query["license"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            version = version ?? data?.version;
            license = license ?? data?.license;
            frameworkFilter = frameworkFilter ?? data?.frameworkFilter;

            if (name == null || version == null || frameworkFilter == null || license == null)
            {
                return new BadRequestObjectResult("Needs more parameters");
            }

            var package = indexer.GetPackage(name, version, frameworkFilter);

            indexer.AssignLicense(package, license);

            return new OkObjectResult("Complete");
        }
    }
}