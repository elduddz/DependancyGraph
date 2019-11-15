using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyGraph.data
{
    public class GreminConnection
    {
        public GremlinClient GraphConnection()
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
