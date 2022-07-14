using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4j.Driver;
using System.Diagnostics;
using System.Linq;

namespace SpaceTracker
{
    public class Neo4JConnector
    {
        private string _uri; 
        private string _username;
        private string _password;
        private bool _blockExecution = false;

        /// <summary>
        /// public constructor
        /// </summary>
        public Neo4JConnector()
        {
            _uri = "YOUR-URL";
            _username = "YOUR-USERNAME";
            _password = "YOUR-PASSWORD";
        }

        public void RunCypherQuery(string query)
        {
            if (_blockExecution)
            {
                return;
            }
            IDriver driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_username, _password));
            using (var session = driver.Session())
            {
                session.Run(query);
            }
        }

        /// <summary>
        /// performs a cypher query
        /// </summary>
        /// <param name="query"></param>
        public async Task RunCypherQueryAsync(string query)
        {
            if (_blockExecution)
            {
                return;
            }
            IDriver driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_username, _password));
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));

            try
            {
                IResultCursor cursor = await session.RunAsync(query);
                await cursor.ConsumeAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
            await driver.CloseAsync();

        }

    }
}
