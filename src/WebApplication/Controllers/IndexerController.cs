using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DependencyGraph;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class IndexerController : ControllerBase
    {
        private ILogger _log;

        public IndexerController(ILogger log)
        {
            _log = log;
        }
        [HttpPost]
        public void StartPoint(string name, string version, string frameworkFilter)
        {
            var indexer = new IndexerService(_log);

            indexer.GetPackage(name, version, frameworkFilter);

        }
    }
}