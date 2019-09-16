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
    public static class Function1
    {
        private static Container container;
        private static ILogger _log;

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
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

            GetPackage(name, version, frameworkFilter);

            AssignLicense(name, version, license);

            return new OkObjectResult("Complete");
        }

        private static void AssignLicense(string name, string version, string license)
        {
            _log.LogInformation($"Assign License: {name}:{version} - {license}");
            using (var gremlinClient = GraphConnection())
            {
                var g = Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));
                var l = gremlinClient.SubmitWithSingleResultAsync<dynamic>($"g.{g.V().Has("license", "id", license).ToGremlinQuery()}").Result;

                if (l == null)
                {
                    l = gremlinClient.SubmitAsync<dynamic>($"g.{g.AddV("license").Property("id", license).Property("Name", license).ToGremlinQuery()}").Result;
                }

                var command = $"V('{name}:{version}').AddE('licensed').To(__.V('{license}'))";
                var v = gremlinClient.SubmitAsync<dynamic>($"g.{command}").Result;
            }
        }

        private static void GetPackage(string name, string version, string frameworkFilter)
        {
            _log.LogInformation($"Get Package: {name}:{version}");

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

                var packageUrl = (xml.GetElementsByTagName("content")[0]).Attributes["src"].InnerText;

                StorePackage(packageName, packageVersion, packageLicense, packageUrl);

                var depends = xml.GetElementsByTagName("d:Dependencies")[0].InnerText;

                if (depends.Length > 0)
                {
                    foreach (var depend in depends.Split('|'))
                    {
                        var parts = depend.Split(':');

                        var frameworkPart = parts[2];

                        if (frameworkPart.Equals(frameworkFilter, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string namePart = parts[0];
                            string versionPart = parts[1];


                            if (!string.IsNullOrEmpty(namePart))
                            {
                                _log.LogInformation($"Getting Dependent {namePart}:{versionPart}");

                                GetPackage(name: namePart, version: versionPart, frameworkFilter);
                                DependsOn(parent: $"{packageName}:{packageVersion}",
                                    dependent: $"{namePart}:{versionPart}");
                            }
                        }
                    }
                }

            }
        }

        private static void DependsOn(string parent, string dependent)
        {
            _log.LogInformation($"Add DependsOn: {parent}:{dependent}");

            using (var gremlinClient = GraphConnection())
            {
                var g = Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));
                var command = $"V('{parent}').AddE('dependsOn').To(__.V('{dependent}'))";
                var result = gremlinClient.SubmitAsync<dynamic>($"g.{command}").Result;
                _log.LogInformation("DependsOn done");
            }
        }

        private static void StorePackage(string packageName, string packageVersion, string packageLicense, string packageUrl)
        {
            _log.LogInformation($"Store Package: {packageName}:{packageVersion}");

            using (var gremlinClient = GraphConnection())
            {
                var g = Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));

                var check = g.V().Has("package", "id", $"{packageName}:{packageVersion}");

                var v = gremlinClient.SubmitWithSingleResultAsync<dynamic>($"g.{check.ToGremlinQuery()}").Result;

                if (v == null)
                {
                    _log.LogInformation("Package Exists");
                    var command = g.AddV("package")
                        .Property("id", $"{packageName}:{packageVersion}")
                        .Property("Name", packageName)
                        .Property("Version", packageVersion)
                        .Property("LicenseUrl", packageLicense)
                        .Property("DownloadUrl", packageUrl);

                    v = gremlinClient.SubmitWithSingleResultAsync<dynamic>($"g.{command.ToGremlinQuery()}").Result;
                }

                _log.LogInformation("Store Done");

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