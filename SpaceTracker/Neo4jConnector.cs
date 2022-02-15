using System;
using Neo4j.Driver;

namespace SpaceTracker
{
    public class Neo4JConnector
    {
        private string _username; 
        private string _password;

        /// <summary>
        /// public constructor
        /// </summary>
        public Neo4JConnector()
        {
            _username = "neo4j";
            _password = "password";
        }


        /// <summary>
        /// performs a cypher query
        /// </summary>
        /// <param name="query"></param>
        public async void RunCypherQuery(string query)
        {
            IDriver driver = GraphDatabase.Driver("neo4j://localhost:7687", AuthTokens.Basic(_username, _password));
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));

            try
            {
                IResultCursor cursor = await session.RunAsync(query);
                IResultSummary res = await cursor.ConsumeAsync();
            }
            finally
            {
                await session.CloseAsync();
            }
            await driver.CloseAsync();
            
        }
    }
}
