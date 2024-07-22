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
            CreateTable();
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
                // for diameter reference dimensions: remove the ref in the name so it can be looked up
                if (annotationName.EndsWith("_REF")) annotationName = annotationName.Replace("_REF", "");

                DataRow[] dimensionData = DimensionPositions.Select($"TOTAL_NAME = '{annotationName}@{totalViewName}'");
                if (dimensionData.Length != 0)
                {
                    // Move dimension
                    DrawingControllerTools.MoveDimension(annotation, viewOutline, (string)dimensionData[0]["REL_SIDE"], (double.Parse((string)dimensionData[0]["REL_X"], CultureInfo.InvariantCulture), double.Parse((string)dimensionData[0]["REL_Y"], CultureInfo.InvariantCulture)));
                }

                // Tolerance dimension
                DataRow[] toleranceData = ToleranceData.Select($"SOFTWARE_NAME = '{annotationName}'");
                // if the tolerance name is present in the tolerance sheet, set the tolerance
                if (toleranceData.Length != 0) DrawingControllerTools.SetDimensionTolerance(viewDimensions[i], toleranceData[0]);
            }

            DrawingControllerTools.HideDimensions(SheetName, "SIDE_VIEW", new string[] { "LOF@LENGTH_REF", "BodyLength@LENGTH_REF" });
            SwModel.Extension.SelectAll();
            // auto align dimensions
            SwModel.Extension.AlignDimensions(0, -0.1);
        }

        private void CreateTable()
        {
            int maxRows = 20;
            DataRow[] toleranceData = ToleranceData.Select("TOL_TYPE = 'TABLE_VAL' OR TOL_TYPE = 'TABLE_TEXT'");
            int count = toleranceData.Length;
            int cols = (int)Math.Ceiling((float)count / maxRows);
            double colWidth = (16.5 - 0.5) / cols;
            int rows = cols > 1 ? maxRows : count;
            // x = 0.5in, y = 10.5 in
            TableAnnotation table = Drawing.InsertTableAnnotation2(false, 0.5.ConvertToMeters(), 10.5.ConvertToMeters(), 1, "", rows, cols);
            // remove table borders
            table.BorderLineWeight = -1;
            table.GridLineWeight = -1;
            // left-align text
            table.TextHorizontalJustification = 1;
            // set text color
            table.GetAnnotation().Color = 255;
            // set column widths
            table.SetColumnWidth(-2, colWidth.ConvertToMeters(), 0);

            for (int i = 0; i < count; i++)
            {
                DataRow data = toleranceData[i];
                string cellText;
                if ((string)data["TOL_TYPE"] == "TABLE_TEXT")
                    cellText = (string)data["TABLE_TEXT"];
                else
                    cellText = $"{(string)data["NAME"]}: {(string)data["MIN_VAL"]} - {(string)data["MAX_VAL"]}";

                int rowIndex = i % rows;
                int colIndex = (int)Math.Floor((float)i / rows);

                table.Text[rowIndex, colIndex] = cellText;
            }
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
