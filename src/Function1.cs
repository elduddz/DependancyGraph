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

            string connectionString = Environment.GetEnvironmentVariable("cosmosDatabase");
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

                var packageName = xml.GetElementsByTagName("d:Id");

                var pacakgeVersion = xml.GetElementsByTagName("d:Version");

                var packageLicense = xml.GetElementsByTagName("d:LicenseUrl");

                var depends = xml.GetElementsByTagName("d:Dependencies");

                log.LogInformation(depends.Count.ToString());


                var package = new Package()
                {
                    id = Guid.NewGuid().ToString(),
                    Name = packageName.Item(0).InnerText,
                    Versions = new PackageVersion[]
                    {
                        new PackageVersion()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Version = pacakgeVersion.Item(0).InnerText,
                            Dependancies = null,
                            License = new License()
                            {
                                Id = Guid.NewGuid().ToString(),
                                uri = packageLicense.Item(0).InnerText,
                                LicenseType = new LicenseType()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = "UNKNOWN"
                                }
                            }
                        }
                    }
                };

                var cosmosClient = new CosmosClient(connectionString);

                var database = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).Result.Database;

                var container = database.CreateContainerIfNotExistsAsync(containerId, "/Name").Result.Container;


                var saved = container.UpsertItemAsync<Package>(package, new PartitionKey(package.Name)).Result;

                return new OkObjectResult(saved);
            }
        }
    }
}