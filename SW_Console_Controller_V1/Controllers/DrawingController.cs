using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace SW_Console_Controller_V1.Controllers
{
    internal class DrawingController : ModelController
    {
        public DrawingDoc Drawing;
        public DataTable DimensionPositions;
        public ToleranceSheetProcessor ToleranceProcessor;
        public DataTable ToleranceData;
        public string SheetName = "NORMAL";
        public DrawingController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, DrawingDoc drawing, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            Drawing = drawing;
            ToleranceProcessor = new ToleranceSheetProcessor(properties);
            ToleranceData = ToleranceProcessor.GetToleranceData();
            LoadTables();
            UpdateDrawing();
        }

        private void LoadTables()
        {
            DimensionPositions = new DataTable();
            var fs = new FileStream(Path.Combine(Properties.MasterPath, $"{Properties.DimensionFileName}.csv"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (var reader = new StreamReader(fs))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                using (var dr = new CsvDataReader(csv))
                {
                    DimensionPositions.Load(dr);
                }
            }
        }

        private void UpdateDrawing()
        {
            switch (SheetName)
            {
                case "NORMAL":
                    SetNormalSheetDimensions();
                    break;
            }
        }

        private void SetViewDimensions(View[] views, string viewName)
        {
            // find view based on sheet type (class property) and input view name
            string totalViewName = $"{SheetName}:{viewName}";
            View view = views.Where(e => e.GetName2() == totalViewName).ToArray()[0];
            double[] viewOutline = view.GetOutline();
            object[] dimensionsTemp = view.GetDisplayDimensions();
            //same thing as with the views array, cannot cast the array directly
            DisplayDimension[] viewDimensions = Array.ConvertAll(dimensionsTemp, d => (DisplayDimension)d);
            for (int i = 0; i < viewDimensions.Length; i++)
            {
                // Find dimension in sheet
                Annotation annotation = viewDimensions[i].GetAnnotation();
                string annotationName = annotation.GetName();
                if (annotationName.EndsWith("_REF")) annotationName = annotationName.Replace("_REF", "");

                DataRow[] dimensionData = DimensionPositions.Select($"TOTAL_NAME = '{annotationName}@{totalViewName}'");
                if (dimensionData.Length != 0)
                {
                    // Move dimension
                    DrawingControllerTools.MoveDimension(annotation, viewOutline, (string)dimensionData[0]["REL_SIDE"], (double.Parse((string)dimensionData[0]["REL_X"], CultureInfo.InvariantCulture), double.Parse((string)dimensionData[0]["REL_Y"], CultureInfo.InvariantCulture)));
                }

                // Tolerance dimension
                //Dimension dim = viewDimensions[i].GetDimension2(0);

                DataRow[] toleranceData = ToleranceData.Select($"SOFTWARE_NAME = '{annotationName}'");
                // if the tolerance name is present in the tolerance sheet, set the tolerance
                if (toleranceData.Length != 0) DrawingControllerTools.SetDimensionTolerance(viewDimensions[i], toleranceData[0]);

                StoreAnnotationData(annotation);
            }

            DrawingControllerTools.HideDimensions(SheetName, "SIDE_VIEW", new string[] { "LOF@LENGTH_REF", "BODY_LENGTH@LENGTH_REF" });
        }

        private void StoreAnnotationData(Annotation annotation)
        {
            // for display dimensions, the position is the xyz coordinate of the top left corner
            double[] annotationPosition = annotation.GetPosition();
        }

        private void SetNormalSheetDimensions()
        {
            Sheet mainSheet = Drawing.Sheet[SheetName];
            mainSheet.SetTemplateName(Path.Combine(Properties.MasterPath, "drawing formats", $"{Properties.DrawingType}.slddrt"));
            mainSheet.ReloadTemplate(true);
            // for some reason casting this to View[] is not possible, despite the elements being Views
            object[] viewsTemp = mainSheet.GetViews();
            View[] views = Array.ConvertAll(viewsTemp, v => (View)v);
            SetViewDimensions(views, "SIDE_VIEW");
        }
    }
}
