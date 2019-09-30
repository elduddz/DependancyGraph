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

namespace DependencyGraph
{
    public static class DropAll
    {
        private static Container container;
        private static ILogger _log;

        [FunctionName("DropAll")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            _log = log;
            _log.LogInformation("C# HTTP trigger function processed a request.");

            using (var gremlinClient = GraphConnection())
            {
                var g = Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));

                var command = g.V().Drop();

                gremlinClient.SubmitAsync($"g.{command.ToGremlinQuery()}").Wait();
            }
            return new OkObjectResult("Done");

        }

        private static ResultSet<dynamic> GetLicenseUrlsForLicense(string license)
        {
            _log.LogInformation($"Assigned License: {license}");
            using (var gremlinClient = GraphConnection())
            {
                var g = Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));

                var command = g.V().HasLabel("license").In("licensed").Values<string>("LicenseUrl").ToGremlinQuery();//.Has("Name", license).In("licensed");

                return gremlinClient.SubmitAsync<dynamic>($"g.{command}").Result;
            }
        }

        private static GremlinClient GraphConnection()
        {
            var connectionString = Environment.GetEnvironmentVariable("connectionString");
            var password = Environment.GetEnvironmentVariable("key");
            var databaseId = Environment.GetEnvironmentVariable("databaseId");
            var containerId = Environment.GetEnvironmentVariable("containerId");

            var gremlinServer = new GremlinServer(connectionString, 443, true, $"/dbs/{databaseId}/colls/{containerId}", password);
            var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);

            return gremlinClient;
        }
    }
}