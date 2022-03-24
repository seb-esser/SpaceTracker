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

        private Neo4JConnector Neo4jConnector;
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
        public void CreateInitialGraph(Document doc)
        {
            // Filename  
            string fileName = "neo4j_commands.txt";

            string cmds = "";


            // -- Rooms and walls adjacent to room -- // 
            RoomFilter filter = new RoomFilter();

            // Apply the filter to the elements in the active document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> rooms = collector.WherePasses(filter).ToElements();


            foreach (var element in rooms)
            {
                var room = (Room)element;

                // capture result
                Debug.WriteLine("Room: " + room.Name);

                string cy = "MERGE (r:Room{Name: \"" + room.Name + "\", ElementId: " + room.Id + "})";
                Neo4jConnector.RunCypherQuery(cy);
                cmds += cy + "\n";

                string sql = "INSERT INTO Room (ElementId, Name) VALUES (" + room.Id + ", '" + room.Name + "\');";
                SqLiteConnector.runSQLQuery(sql);

                // create level node
                var current_level = room.Level;
                Debug.WriteLine("Level: " + current_level.Name);

                cy = "MATCH (r:Room{ElementId:" + room.Id + "}) MERGE (l:Level{Name: \"" + current_level.Name + "\", ElementId: " + current_level.Id + "}) MERGE (l)-[:CONTAINS]->(r)";
                Neo4jConnector.RunCypherQuery(cy);
                cmds += cy + "\n";

                //Insert level into tables
                sql = "INSERT INTO Level (ElementId, Name) VALUES (" + current_level.Id + ", '" + current_level.Name + "');";
                SqLiteConnector.runSQLQuery(sql);
                sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + current_level.Id + ", '" + room.Id + "');";
                SqLiteConnector.runSQLQuery(sql);

                IList<IList<BoundarySegment>> boundaries
                    = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                
                foreach (IList<BoundarySegment> b in boundaries)
                {
                    // loop over all elements adjacent to current room
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
                            Debug.WriteLine("\tNeighbor Type: Wall - ID: " + neighbor.Id);

                            cy = "MATCH (r: Room{ElementId:" + room.Id + "}) MERGE (w:Wall{ElementId: " + neighbor.Id + ", Name: \""+ neighbor.Name + "\"})  MERGE (w)-[:BOUNDS]->(r)"; 
                            Neo4jConnector.RunCypherQuery(cy);
                            Thread.Sleep(2000);
                            cmds += cy + "\n";

                            // make level connection
                            cy = "MATCH (l:Level{ElementId: " + neighbor.LevelId + "}), (w:Wall{ElementId: " + neighbor.Id + ", Name: \"" + neighbor.Name + "\"}) MERGE (l)-[:CONTAINS]->(w)";
                            Neo4jConnector.RunCypherQuery(cy);
                            cmds += cy + "\n";

                            sql = "INSERT INTO Wall (ElementId, Name) VALUES (" + neighbor.Id + ", '" + neighbor.Name + "');";
                            SqLiteConnector.runSQLQuery(sql);
                            sql = "INSERT INTO bounds (WallId, RoomId) VALUES (" + neighbor.Id + ", " + room.Id + ");";
                            SqLiteConnector.runSQLQuery(sql);
                            // insert level into table
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

            // -- doors -- // 

            Debug.WriteLine("--");

            var doorCollector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors).OfClass(typeof(FamilyInstance));

            var doors = doorCollector.ToElements();


            foreach (var door in doors)
            {
                var inst = (FamilyInstance) door;
                var wall = inst.Host;
                Debug.WriteLine("Door ID: " + door.Id + " - HostId: " + wall.Id );

                string cy = "MATCH (w: Wall{ElementId:" + wall.Id + "})" +
                     "MERGE (d:Door{ElementId:" + inst.Id.IntegerValue + ", Name: \"" + inst.Name + "\" })" +
                     "MERGE (d)-[:CONTAINED_IN]->(w)";
                Neo4jConnector.RunCypherQuery(cy);
                cmds += cy + "\n";

                // make level connection
                cy = "MATCH (d:Door{ElementId:" + inst.Id.IntegerValue + ", Name :\"" + inst.Name + "\" }), (l:Level{ElementId: " + inst.LevelId + "}) MERGE (l)-[:CONTAINS]->(d)";
                Neo4jConnector.RunCypherQuery(cy);
                cmds += cy + "\n";

                string sql = "INSERT INTO Door (ElementId, Name, WallId) VALUES (" + door.Id + ", \"" + door.Name + "\", " + wall.Id + ");";
                SqLiteConnector.runSQLQuery(sql);
                // insert level into table
                sql = "INSERT INTO contains (LevelId, ElementId) VALUES (" + door.LevelId + ", " + door.Id + ");";
                SqLiteConnector.runSQLQuery(sql);
            }

            File.WriteAllText(@"C:\sqlite_tmp\neo4jcmds.txt", cmds);
        }
    }
}