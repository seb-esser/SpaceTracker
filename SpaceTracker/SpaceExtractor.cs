using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SpaceTracker
{
    public class SpaceExtractor
    {

        //private Neo4JConnector Neo4jConnector;
        //private SQLiteConnector SqLiteConnector;
        private readonly CommandManager cmdManager;


        /// <summary>
        /// Dflt constructor
        /// </summary>
        public SpaceExtractor()
        {
            var Neo4jConnector = new Neo4JConnector();
            var SqLiteConnector = new SQLiteConnector();
            cmdManager = new CommandManager(Neo4jConnector, SqLiteConnector);
        }

        /// <summary>
        /// Extracts the existing situation from a model 
        /// </summary>
        /// <param name="doc"></param>
        public void CreateInitialGraph(Document doc)
        {
            // create stopwatch to measure the elapsed time
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Debug.WriteLine("#--------#\nTimer started.\n#--------#");
            
            // Get all levels
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> levels = collector.OfClass(typeof(Level)).ToElements();

            // Iterate over all levels
            foreach (var lvl in levels)
            {
                Debug.WriteLine($"Level: {lvl.Name}, ID: {lvl.Id}");

                string cy = "MERGE (l:Level{Name: \"" + lvl.Name + "\", ElementId: " + lvl.Id + "})";
                cmdManager.cypherCommands.Add(cy);

                string sql = "INSERT INTO Level (ElementId, Name) VALUES (" + lvl.Id + ", '" + lvl.Name + "');";
                cmdManager.sqlCommands.Add(sql);

                // get all Elements of type Room in the current level
                ElementLevelFilter lvlFilter = new ElementLevelFilter(lvl.Id);
                collector = new FilteredElementCollector(doc);
                IList<Element> rooms = collector.WherePasses(new RoomFilter()).WherePasses(lvlFilter).ToElements();

                // Iterate over all rooms in that level
                foreach (var element in rooms)
                {
                    var room = (Room)element;

                    // capture result
                    Debug.WriteLine($"Room: {room.Name}, ID: {room.Id}");

                    cy = "MATCH (l:Level{ElementId:" + room.LevelId + "}) " +
                         "MERGE (r:Room{Name: \"" + room.Name + "\", ElementId: " + room.Id + "}) " +
                         "MERGE (l)-[:CONTAINS]->(r)";
                    cmdManager.cypherCommands.Add(cy);

                    sql = "INSERT INTO Room (ElementId, Name) VALUES (" + room.Id + ", '" + room.Name + "');";
                    cmdManager.sqlCommands.Add(sql);
                    //make level connection
                    sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + room.LevelId + ", '" + room.Id + "');";
                    cmdManager.sqlCommands.Add(sql);

                    // get all boundaries
                    IList<IList<BoundarySegment>> boundaries
                    = room.GetBoundarySegments(new SpatialElementBoundaryOptions());


                    foreach (IList<BoundarySegment> b in boundaries)
                    {
                        // Iterate over all elements adjacent to current room
                        foreach (BoundarySegment s in b)
                        {

                            // get neighbor element
                            ElementId neighborId = s.ElementId;
                            if (neighborId.IntegerValue == -1)
                            {
                                Debug.WriteLine("Something went wrong when extracting Element ID " + neighborId);
                                continue;
                            }

                            Element neighbor = doc.GetElement(neighborId);

                            if (neighbor is Wall)
                            {
                                Debug.WriteLine($"\tNeighbor Type: Wall - ID: {neighbor.Id}");

                                cy = "MATCH (r:Room{ElementId:" + room.Id + "}) " +
                                     "MATCH (l:Level{ElementId:" + neighbor.LevelId + "}) " +
                                     "MERGE (w:Wall{ElementId: " + neighbor.Id + ", Name: \"" + neighbor.Name + "\"})  " +
                                     "MERGE (l)-[:CONTAINS]->(w)-[:BOUNDS]->(r)";
                                cmdManager.cypherCommands.Add(cy);

                                // create the sql queries, and then check if they have already been executed
                                // this is sometimes necessary because walls can be adjacent to multiple rooms
                                sql = "INSERT INTO Wall (ElementId, Name) VALUES (" + neighbor.Id + ", '" + neighbor.Name + "');";
                                if (!cmdManager.sqlCommands.Contains(sql))
                                {
                                    cmdManager.sqlCommands.Add(sql);
                                }                                
                                sql = "INSERT INTO bounds (WallId, RoomId) VALUES (" + neighbor.Id + ", " + room.Id + ");";
                                if (!cmdManager.sqlCommands.Contains(sql))
                                {
                                    cmdManager.sqlCommands.Add(sql);
                                }
                                // make level connection
                                sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + neighbor.LevelId + ", " + neighbor.Id + ");";
                                if (!cmdManager.sqlCommands.Contains(sql))
                                {
                                    cmdManager.sqlCommands.Add(sql);
                                }
                            }

                            else
                            {
                                Debug.WriteLine("\tNeighbor Type: Undefined - ID: " + neighbor.Id);
                            }
                        }
                    }
                }
                Debug.WriteLine("--");

                // get all doors at current level
                var doorCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance)).WherePasses(lvlFilter);

                var doors = doorCollector.ToElements();

                // Iterate over all doors at current level
                foreach (var door in doors)
                {
                    var inst = (FamilyInstance)door;
                    var wall = inst.Host;
                    Debug.WriteLine($"Door ID: {door.Id}, HostId: {wall.Id}");

                    cy = "MATCH (w:Wall{ElementId:" + wall.Id + "})" +
                         "MATCH (l:Level{ElementId:" + door.LevelId + "})" +
                         "MERGE (d:Door{ElementId:" + inst.Id.IntegerValue + ", Name: \"" + inst.Name + "\" })" +
                         "MERGE (l)-[:CONTAINS]->(d)-[:CONTAINED_IN]->(w)";
                    cmdManager.cypherCommands.Add(cy);


                    sql = "INSERT INTO Door (ElementId, Name, WallId) VALUES (" + door.Id + ", '" + door.Name + "', " + wall.Id + ");";
                    cmdManager.sqlCommands.Add(sql);
                    // insert level into table
                    sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + door.LevelId + ", " + door.Id + ");";
                    cmdManager.sqlCommands.Add(sql);
                }
            }

            // write commands to file
            var cyCmds = string.Join("\n", cmdManager.cypherCommands);
            File.WriteAllText(@"C:\sqlite_tmp\neo4jcmds.txt", cyCmds);

            var sqlCmds = string.Join("\n", cmdManager.sqlCommands);
            File.WriteAllText(@"C:\sqlite_tmp\neo4jcmds.txt", sqlCmds);

            // print out the elapsed time and stop the timer
            Debug.WriteLine($"#--------#\nTimer stopped: {timer.ElapsedMilliseconds}ms\n#--------#");
            timer.Stop();
        }

        // Deletes all previously existing data (convenient for debugging)
        public void DeleteExistingGraph()
        {
            Debug.WriteLine("Existing graph is being deleted...");
            // Delete all neo4j data
            string cy = "MATCH (n) DETACH DELETE n";
            cmdManager.cypherCommands.Add(cy);

            Debug.WriteLine("Existing table data is being deleted...\n");
            // Delete all sqlite data
            string sql = "DELETE FROM Level";
            cmdManager.sqlCommands.Add(sql);
            sql = "DELETE FROM Room";
            cmdManager.sqlCommands.Add(sql);
            sql = "DELETE FROM Wall";
            cmdManager.sqlCommands.Add(sql);
            sql = "DELETE FROM Door";
            cmdManager.sqlCommands.Add(sql);
            sql = "DELETE FROM contains";
            cmdManager.sqlCommands.Add(sql);
            sql = "DELETE FROM bounds";
            cmdManager.sqlCommands.Add(sql);

        }
    }
}