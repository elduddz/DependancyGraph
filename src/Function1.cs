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
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Remote;
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

namespace DependancyGraph
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string version = req.Query["version"];

            string connectionString = Environment.GetEnvironmentVariable("connectionString");
            string primaryConnectionString = Environment.GetEnvironmentVariable("primaryConnectionString");
            string password = Environment.GetEnvironmentVariable("key");
            string databaseId = Environment.GetEnvironmentVariable("databaseId");
            string containerId = Environment.GetEnvironmentVariable("containerId");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            version = version ?? data?.version;

            var url = $"https://www.nuget.org/api/v2/Packages(Id='{name}',Version='{version}')";

            var xml = new XmlDocument();

            using (var httpClient = new HttpClient())
            {
                var result = httpClient.GetStreamAsync(url);

                xml.Load(result.Result);

                var dns = xml.GetNamespaceOfPrefix("d");
                var mns = xml.GetNamespaceOfPrefix("m");

                var properties = xml.GetElementsByTagName("m:properties");

                var packageName = xml.GetElementsByTagName("d:Id")[0].InnerText;

                var packageVersion = xml.GetElementsByTagName("d:Version")[0].InnerText;

                var packageLicense = xml.GetElementsByTagName("d:LicenseUrl")[0].InnerText;

                var depends = xml.GetElementsByTagName("d:Dependencies");

                log.LogInformation(depends.Count.ToString());


                var package = new Package()
                {
                    Id = $"{packageName}:{packageVersion}",
                    Name = packageName,
                    Version = packageVersion,
                    LicenseUrl = packageLicense,
                };

                var cosmosClient = new CosmosClient(primaryConnectionString);
                var container = cosmosClient.GetContainer(databaseId, containerId);
                var itemResponse = container.UpsertItemAsync<Package>(package).Result;


                var gremlinServer = new GremlinServer(connectionString, 443, true, $"/dbs/{databaseId}/colls/{containerId}", password);

                var gremlinClient = new GremlinClient(gremlinServer);

                var remoteConnection = new DriverRemoteConnection(gremlinClient);

                var g = Traversal().WithRemote(remoteConnection);

                return new OkObjectResult(itemResponse);
            }
        }
    }
}