using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        #region register events

        /// <summary>
        /// Catch startup and mount event handlers
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            Debug.WriteLine("[SpaceTracker] Mounting... ");
            try
            {
                application.ControlledApplication.DocumentCreated +=
                    new EventHandler<DocumentCreatedEventArgs>(documentCreated);
                application.ControlledApplication.DocumentOpened +=
                    new EventHandler<Autodesk.Revit.DB.Events.DocumentOpenedEventArgs>(documentOpened);
                application.ControlledApplication.DocumentChanged +=
                    new EventHandler<DocumentChangedEventArgs>(documentChanged);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.WriteLine("Something went wrong during the registration process. ");

                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// remove mounted events
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened -= documentOpened;
            application.ControlledApplication.DocumentChanged -= documentChanged;
            application.ControlledApplication.DocumentCreated -= documentCreated;
            return Result.Succeeded;
        }

        #endregion

        #region Event handler

        private void documentChanged(object sender, DocumentChangedEventArgs e)
        {
            var addedElementIds = e.GetAddedElementIds();
            var deletedElementIds = e.GetDeletedElementIds();
            var modifiedElementIds = e.GetModifiedElementIds();
            
            
        }

        private void documentCreated(object sender, DocumentCreatedEventArgs e)
        {
            // get document from event args.
            Document doc = e.Document;

            // nothing to query from the existing Revit model except the "outside" node

        }

        private void documentOpened(object sender, DocumentOpenedEventArgs e)
        {
            
            // get document from event args.
            Document doc = e.Document;

            var extractor = new SpaceExtractor();
            // Delete existing data
            extractor.DeleteExistingGraph();
            extractor.CreateInitialGraph(doc);

            // get all spaces and build graph from existing model
        }

        #endregion
    }
}