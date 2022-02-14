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

                int iBoundary = 0;
                foreach (IList<BoundarySegment> b in boundaries)
                {
                    ++iBoundary;
                    var iSegment = 0;
                    foreach (BoundarySegment s in b)
                    {
                        ++iSegment;
                        // get neighbor element
                        ElementId neighborId = s.ElementId;
                        Element neighbor = doc.GetElement(neighborId);

                        Curve curve = s.GetCurve();

                        if (neighbor is Room)
                        {
                            Debug.WriteLine("\tNeighbor Type: Room - ID:" + neighbor.Id);
                        }

                        else if (neighbor is Wall)
                        {
                            Debug.WriteLine("\tNeighbor Type: Wall - ID: " + neighbor.Id);
                        }

                        else
                        {
                            Debug.WriteLine("\tNeighbor Type: Undefined - ID: " + neighbor.Id);
                        }
                    }
                }
            }

        }
    }
}