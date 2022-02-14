using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SpaceTracker
{
    public class SpaceExtractor
    {
        private void extractAdjacentRooms(Room room, ref Document doc)
        {
            IList<IList<BoundarySegment>> boundaries
                = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            int n = boundaries.Count;

            int iBoundary = 0;

            foreach (IList b in boundaries)
            {
                ++iBoundary;
                var iSegment = 0;
                foreach (BoundarySegment s in b)
                {
                    ++iSegment;
                    ElementId neighbourId = s.ElementId;

                    Element neighbour = doc.GetElement(neighbourId);

                    Curve curve = s.GetCurve();
                    
                    if (neighbour is Room)
                    {
                        Debug.WriteLine(neighbour.Id);
                    }
                }
            }

        }
    }
}