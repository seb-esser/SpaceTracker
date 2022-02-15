using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SpaceTracker
{
    public class SpaceExtractor
    {

        private Neo4JConnector Connector; 

        /// <summary>
        /// Dflt constructor
        /// </summary>
        public SpaceExtractor()
        {
            Connector = new Neo4JConnector();
        }

        /// <summary>
        /// Extracts the existing situation from a model 
        /// </summary>
        /// <param name="doc"></param>
        public void CreateInitialGraph(Document doc)
        {
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
                Connector.RunCypherQuery(cy);

                
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
                            Connector.RunCypherQuery(cy);
                            Thread.Sleep(500);
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

                Connector.RunCypherQuery("MATCH (w: Wall{ElementId:" + wall.Id + "})" + 
                                         "MERGE (d:Door{ElementId:" + inst.Id.IntegerValue + ", Name: \"" +inst.Name + "\" })" + 
                                         "MERGE (d)-[:CONTAINED_IN]->(w)");
                
            }

        }
    }
}