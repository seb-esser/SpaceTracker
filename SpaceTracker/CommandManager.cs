using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SpaceTracker
{
    class CommandManager
    {
        public ObservableCollection<string> cypherCommands;
        public ObservableCollection<string> sqlCommands;
        public Neo4JConnector neo4jConnector;
        public SQLiteConnector sqlConnector;

        public CommandManager(Neo4JConnector connector1, SQLiteConnector connector2)
        {
            neo4jConnector = connector1;
            sqlConnector = connector2;

            cypherCommands = new ObservableCollection<string>();
            sqlCommands = new ObservableCollection<string>();

            cypherCommands.CollectionChanged += GraphCommandUpdate;
            sqlCommands.CollectionChanged += SqlCommandUpdate;
        }

        private void SqlCommandUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            sqlConnector.runSQLQuery(sqlCommands[e.NewStartingIndex]);
        }

        private void GraphCommandUpdate(object sender, NotifyCollectionChangedEventArgs e)
        {
            neo4jConnector.RunCypherQuery(cypherCommands[e.NewStartingIndex]);
        }
    }
}
