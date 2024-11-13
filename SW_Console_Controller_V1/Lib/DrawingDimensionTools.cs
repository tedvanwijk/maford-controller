using OfficeOpenXml;
using OfficeOpenXml.Export.ToDataTable;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SW_Console_Controller_V1.Lib
{
    internal class DrawingDimensionTools
    {
        public static ModelDoc2 Model;
        public static ModelDoc2 DrawingModel;
        public static DrawingDoc Drawing;
        public static SelectionMgr SelectionMgr;
        public static ModelDocExtension ModelExtension;
        public static ModelDocExtension DrawingExtension;
        public static Properties Properties;
        private static DataTable MarkingData;
        private static DataTable InsertionData;
        private static DataTable ViewData;
        private static string[] EnabledViews;
        private static string[] DisabledViews;

        static public void LoadDimensionData()
        {
            SelectionMgr = Model.SelectionManager;
            ModelExtension = Model.Extension;
            DrawingExtension = DrawingModel.Extension;

            string path = Properties.DimensionPath;

            FileInfo fileInfo = new FileInfo(path);
            ExcelPackage package = new ExcelPackage(fileInfo);
            ExcelWorksheet markingSheet = package.Workbook.Worksheets[$"{Properties.ToolType}_MARKING"];
            ExcelWorksheet generalMarkingSheet = package.Workbook.Worksheets["General_MARKING"];
            ExcelWorksheet insertionSheet = package.Workbook.Worksheets[$"{Properties.ToolType}_INSERTION"];
            ExcelWorksheet generalInsertionSheet = package.Workbook.Worksheets["General_INSERTION"];
            ExcelWorksheet viewSheet = package.Workbook.Worksheets["General_VIEWS"];

            ToDataTableOptions generalMarkingOptions = ToDataTableOptions.Create();
            generalMarkingOptions.EmptyRowStrategy = EmptyRowsStrategy.Ignore;
            generalMarkingOptions.FirstRowIsColumnNames = true;
            MarkingData = generalMarkingSheet.Cells["A:D"].ToDataTable(generalMarkingOptions);

            ToDataTableOptions markingOptions = ToDataTableOptions.Create();
            markingOptions.EmptyRowStrategy = EmptyRowsStrategy.Ignore;
            markingOptions.FirstRowIsColumnNames = true;
            MarkingData.Merge(markingSheet.Cells["A:D"].ToDataTable(markingOptions), true, MissingSchemaAction.Ignore);

            ToDataTableOptions generalInsertionOptions = ToDataTableOptions.Create();
            generalInsertionOptions.EmptyRowStrategy = EmptyRowsStrategy.Ignore;
            generalInsertionOptions.FirstRowIsColumnNames = true;
            InsertionData = generalInsertionSheet.Cells["A:D"].ToDataTable(generalInsertionOptions);

            ToDataTableOptions insertionOptions = ToDataTableOptions.Create();
            insertionOptions.EmptyRowStrategy = EmptyRowsStrategy.Ignore;
            insertionOptions.FirstRowIsColumnNames = true;
            InsertionData.Merge(insertionSheet.Cells["A:D"].ToDataTable(insertionOptions), true, MissingSchemaAction.Ignore);

            ToDataTableOptions viewOptions = ToDataTableOptions.Create();
            viewOptions.EmptyRowStrategy = EmptyRowsStrategy.Ignore;
            viewOptions.FirstRowIsColumnNames = true;
            ViewData = viewSheet.Cells["A:C"].ToDataTable(viewOptions);
        }

        static public void MarkDimensions()
        {
            string[] configs = new string[] { "Default", "Blank" };

            foreach (string config in configs)
            {
                Model.ShowConfiguration2(config);
                DataRow[] entries = MarkingData.Select();

                for (int i = 0; i < entries.Length; i++)
                {
                    Model.ClearSelection2(true);
                    DataRow entry = entries[i];
                    string dimensionName = $"{entry["DIMENSION"]}@{entry["SKETCH"]}@{Properties.PartFileName}";
                    Select(dimensionName, "DIMENSION");
                    if (!ValidateRule(entry["RULE"])) continue;

                    DisplayDimension dim = SelectionMgr.GetSelectedObject6(1, -1);
                    dim.MarkedForDrawing = (string)entry["ENABLE/DISABLE"] == "ENABLE";
                }
            }
            Model.ShowConfiguration2("Default");
        }

        static public void AddDimensions()
        {
            for (int i = 0; i < EnabledViews.Length; i++)
            {
                DataRow[] entries = InsertionData.Select($"DRAWING_VIEW = '{EnabledViews[i]}'");
                List<string> features = new List<string>();
                foreach (DataRow entry in entries)
                {
                    DrawingModel.ClearSelection2(true);

                    if (!ValidateRule(entry["RULE"])) continue;

                    string featureName = $"{entry["SKETCH"]}@{Properties.DrawingFileName}@{EnabledViews[i]}";

                    if (features.Contains(featureName)) continue;

                    DrawingSelect(featureName, "SKETCH");
                    DrawingModel.UnblankSketch();
                    DrawingSelect(featureName, "SKETCH");

                    DrawingSelect(EnabledViews[i], "DRAWINGVIEW", true);

                    features.Add(featureName);
                    object[] annotations = Drawing.InsertModelAnnotations3((int)swImportModelItemsSource_e.swImportModelItemsFromSelectedFeature, (int)swInsertAnnotation_e.swInsertDimensionsMarkedForDrawing, false, false, false, false);

                    if (annotations == null) continue;

                    object exclude = entry["EXCLUDE"];
                    if (exclude == DBNull.Value) continue;

                    string[] excludeNames = Array.ConvertAll(((string)exclude).Split(';'), e => e.Trim());

                    foreach (object a in annotations)
                    {
                        Annotation annotation = (Annotation)a;
                        if (excludeNames.Contains(annotation.GetName())) annotation.Visible = (int)swAnnotationVisibilityState_e.swAnnotationHidden;
                    }
                }
            }
        }

        static public (string[] enabledViews, string[] disabledViews) GetViews()
        {
            List<string> enabledViews = new List<string> ();
            List<string> disabledViews = new List<string> ();

            DataRow[] entries = ViewData.Select();
            foreach (DataRow entry in entries)
            {
                bool enable = (string)entry["ENABLE/DISABLE"] == "ENABLE";
                if (!ValidateRule(entry["RULE"])) enable = !enable;

                if (enable) enabledViews.Add((string)entry["VIEW_NAME"]);
                else disabledViews.Add((string)entry["VIEW_NAME"]);
            }

            EnabledViews = enabledViews.ToArray();
            DisabledViews = disabledViews.ToArray();
            return (EnabledViews, DisabledViews);
        }

        static private bool ValidateRule(object rule)
        {
            bool rulePassed;
            if (rule == DBNull.Value)
            {
                rulePassed = true;
            }
            else
            {
                string ruleString = (string)rule;

                bool negate = ruleString.StartsWith("!");
                if (negate) ruleString = ruleString.Substring(1);

                object ruleProperty = Properties;

                foreach (var prop in ruleString.Split('.').Select(s => ruleProperty.GetType().GetProperty(s)))
                    ruleProperty = prop.GetValue(ruleProperty, null);

                if (ruleProperty is bool) rulePassed = (bool)ruleProperty;
                else rulePassed = false;

                if (negate) rulePassed = !rulePassed;
            }

            return rulePassed;
        }

        static private void Select(string name, string type)
        {
            ModelExtension.SelectByID2(name, type, 0, 0, 0, true, 0, null, 0);
        }

        static public void DrawingSelect(string name, string type, bool append = false)
        {
            DrawingExtension.SelectByID2(name, type, 0, 0, 0, append, 0, null, 0);
        }
    }
}
