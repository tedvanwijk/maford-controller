﻿using OfficeOpenXml;
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
                    DataRow entry = entries[i];

                    string sketchName = (string)entry["SKETCH"];

                    if (sketchName.Contains('[') && sketchName.Contains(']'))
                    {
                        int varStartIndex = sketchName.IndexOf('[');
                        int varEndIndex = sketchName.IndexOf(']');
                        string varName = sketchName.Substring(varStartIndex + 1, varEndIndex - varStartIndex - 1);

                        int max = 0;
                        int min = 0;
                        switch (varName)
                        {
                            case "STEP_COUNT":
                                if (Properties.StepTool) max = Properties.Steps.Length;
                                break;
                        }

                        for (int ii = min; ii < max; ii++)
                        {
                            if (!ValidateRule(entry["RULE"], ii)) continue;

                            Model.ClearSelection2(true);

                            string sketchNameStep = $"{sketchName.Substring(0, varStartIndex)}{ii.ToString()}{sketchName.Substring(varEndIndex + 1)}";
                            string dimensionName = $"{entry["DIMENSION"]}@{sketchNameStep}@{Properties.PartFileName}";
                            Select(dimensionName, "DIMENSION");

                            DisplayDimension dim = SelectionMgr.GetSelectedObject6(1, -1);
                            if (dim == null) continue;
                            dim.MarkedForDrawing = (string)entry["ENABLE/DISABLE"] == "ENABLE";
                        }
                    }
                    else
                    {
                        if (!ValidateRule(entry["RULE"])) continue;
                        Model.ClearSelection2(true);

                        string dimensionName = $"{entry["DIMENSION"]}@{sketchName}@{Properties.PartFileName}";
                        Select(dimensionName, "DIMENSION");

                        DisplayDimension dim = SelectionMgr.GetSelectedObject6(1, -1);
                        if (dim == null) continue;
                        dim.MarkedForDrawing = (string)entry["ENABLE/DISABLE"] == "ENABLE";
                    }
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

                    string sketchName = (string)entry["SKETCH"];

                    if (sketchName.Contains('[') && sketchName.Contains(']'))
                    {
                        int varStartIndex = sketchName.IndexOf('[');
                        int varEndIndex = sketchName.IndexOf(']');
                        string varName = sketchName.Substring(varStartIndex + 1, varEndIndex - varStartIndex - 1);

                        int max = 0;
                        int min = 0;
                        switch (varName)
                        {
                            case "STEP_COUNT":
                                if (Properties.StepTool) max = Properties.Steps.Length;
                                break;
                        }

                        for (int ii = min; ii < max; ii++)
                        {
                            string sketchNameStep = $"{sketchName.Substring(0, varStartIndex)}{ii.ToString()}{sketchName.Substring(varEndIndex + 1)}";
                            string featureName = $"{sketchNameStep}@{Properties.DrawingFileName}@{EnabledViews[i]}";

                            if (features.Contains(featureName)) continue;
                            features.Add(featureName);

                            InsertDimension(featureName, entry, i);
                        }
                    }
                    else
                    {
                        string featureName = $"{entry["SKETCH"]}@{Properties.DrawingFileName}@{EnabledViews[i]}";

                        if (features.Contains(featureName)) continue;
                        features.Add(featureName);

                        InsertDimension(featureName, entry, i);
                    }
                }
            }
        }

        static private void InsertDimension(string featureName, DataRow entry, int viewIndex)
        {
            DrawingSelect(featureName, "SKETCH");
            DrawingModel.UnblankSketch();
            DrawingSelect(featureName, "SKETCH");

            DrawingSelect(EnabledViews[viewIndex], "DRAWINGVIEW", true);

            object[] annotations = Drawing.InsertModelAnnotations3((int)swImportModelItemsSource_e.swImportModelItemsFromSelectedFeature, (int)swInsertAnnotation_e.swInsertDimensionsMarkedForDrawing, false, false, true, false);

            if (annotations == null) return;

            object exclude = entry["EXCLUDE"];
            if (exclude == DBNull.Value) return;

            string[] excludeNames = Array.ConvertAll(((string)exclude).Split(';'), e => e.Trim());

            foreach (object a in annotations)
            {
                Annotation annotation = (Annotation)a;
                if (excludeNames.Contains(annotation.GetName())) annotation.Visible = (int)swAnnotationVisibilityState_e.swAnnotationHidden;
            }
        }

        static public (string[] enabledViews, string[] disabledViews) GetViews()
        {
            List<string> enabledViews = new List<string>();
            List<string> disabledViews = new List<string>();

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

        static private bool ValidateRule(object rule, int currentIteration = 0)
        {
            if (rule == DBNull.Value) return true;

            bool rulePassed = true;

            string[] rules = ((string)rule).Split('&');

            foreach (string ruleStringLoop in rules)
            {
                string ruleString = ruleStringLoop;
                bool currentRulePassed;

                // check if rule should be negated
                bool negate = ruleString.StartsWith("!");
                if (negate) ruleString = ruleString.Substring(1);

                // get property value of rule. Works with nested properties
                dynamic ruleProperty = Properties;

                foreach (string ruleStringSegment in ruleString.Split('.'))
                {
                    if (ruleStringSegment.StartsWith("[") && ruleStringSegment.EndsWith("]"))
                    {
                        // rule segment is an iterator. Property is the element in the array with the given iterator
                        ruleProperty = ruleProperty[currentIteration];
                    } else
                    {
                        // rule segment is a nested property
                        var prop = ruleProperty.GetType().GetProperty(ruleStringSegment);
                        ruleProperty = prop.GetValue(ruleProperty, null);
                    }
                }

                // check if rule is passed
                if (ruleProperty is bool) currentRulePassed = (bool)ruleProperty;
                else currentRulePassed = false;

                if (negate) currentRulePassed = !currentRulePassed;

                if (!currentRulePassed)
                {
                    rulePassed = false;
                    break;
                }
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
