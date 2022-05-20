using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SpaceTracker
{
    public class SpaceExtractor
    {

        public Neo4JConnector Neo4jConnector;
        private SQLiteConnector SqLiteConnector;


        /// <summary>
        /// Dflt constructor
        /// </summary>
        public SpaceExtractor()
        {
            Neo4jConnector = new Neo4JConnector();
            SqLiteConnector = new SQLiteConnector();
        }

        /// <summary>
        /// Extracts the existing situation from a model 
        /// </summary>
        /// <param name="doc"></param>
        public async void CreateInitialGraph(Document doc)
        {
            // List of string for commands
            List<string> cypherCommands = new List<string>();

            // Get all levels
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> levels = collector.OfClass(typeof(Level)).ToElements();

            // Iterate over all levels
            foreach (var lvl in levels)
            {
                Debug.WriteLine($"Level: {lvl.Name}, ID: {lvl.Id}");

                string cy = "MERGE (l:Level{Name: \"" + lvl.Name + "\", ElementId: " + lvl.Id + "})";
                cypherCommands.Add(cy);
                await Neo4jConnector.RunCypherQueryAsync(cy);
                //_ = Neo4jConnector.RunCypherQueryAsync(cy);

                string sql = "INSERT INTO Level (ElementId, Name) VALUES (" + lvl.Id + ", '" + lvl.Name + "');";
                SqLiteConnector.runSQLQuery(sql);

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
                    cypherCommands.Add(cy);
                    await Neo4jConnector.RunCypherQueryAsync(cy);
                    //_ = Neo4jConnector.RunCypherQueryAsync(cy);

                    sql = "INSERT INTO Room (ElementId, Name) VALUES (" + room.Id + ", '" + room.Name + "\');";
                    SqLiteConnector.runSQLQuery(sql);
                    //make level connection
                    sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + room.LevelId + ", '" + room.Id + "');";
                    SqLiteConnector.runSQLQuery(sql);

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
                                cypherCommands.Add(cy);
                                await Neo4jConnector.RunCypherQueryAsync(cy);
                                //_ = Neo4jConnector.RunCypherQueryAsync(cy);

                                sql = "INSERT INTO Wall (ElementId, Name) VALUES (" + neighbor.Id + ", '" + neighbor.Name + "');";
                                SqLiteConnector.runSQLQuery(sql);
                                sql = "INSERT INTO bounds (WallId, RoomId) VALUES (" + neighbor.Id + ", " + room.Id + ");";
                                SqLiteConnector.runSQLQuery(sql);
                                // make level connection
                                sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + neighbor.LevelId + ", " + neighbor.Id + ");";
                                SqLiteConnector.runSQLQuery(sql);
                            }

                            else
                            {
                                try
                                {
                                    Debug.WriteLine("\tNeighbor Type: Undefined - ID: " + neighbor.Id);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e);
                                }

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
                    cypherCommands.Add(cy);
                    await Neo4jConnector.RunCypherQueryAsync(cy);
                    //_ = Neo4jConnector.RunCypherQueryAsync(cy);


                    sql = "INSERT INTO Door (ElementId, Name, WallId) VALUES (" + door.Id + ", \"" + door.Name + "\", " + wall.Id + ");";
                    SqLiteConnector.runSQLQuery(sql);
                    // insert level into table
                    sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + door.LevelId + ", " + door.Id + ");";
                    SqLiteConnector.runSQLQuery(sql);
                }
            }

            // write commands to file
            var cmds = string.Join("\n", cypherCommands);
            File.WriteAllText(@"C:\sqlite_tmp\neo4jcmds.txt", cmds);
        }
    }
}