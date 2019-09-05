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

namespace DependancyGraph
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function,"get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string version = req.Query["version"];

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

                var ns = xml.GetNamespaceOfPrefix("d");

                var depends = xml.GetElementsByTagName("Dependencies", ns);

                log.LogInformation(depends.Count.ToString());

            }


            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        x`}
    }
}
