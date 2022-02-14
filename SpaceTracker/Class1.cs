using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;

namespace SpaceTracker
{
    public class SpaceTrackerClass : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            throw new NotImplementedException();
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            throw new NotImplementedException();
        }
    }
}
