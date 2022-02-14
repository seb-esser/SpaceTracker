using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SpaceTracker
{
    public class SpaceExtractor
    {
        /// <summary>
        /// Dflt constructor
        /// </summary>
        public SpaceExtractor()
        {
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
                Debug.WriteLine("Room: " + element.Name);
                var room = (Room)element;
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

            var doorCollector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors);
            var doors = doorCollector.ToElements();

            foreach (var door in doors)
            {
                var inst = (FamilyInstance) door;
                var host = inst.Host;
                Debug.WriteLine("Door ID: " + door.Id + " - HostId: " + host.Id );
            }

        }
    }
}