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
using SolidWorks.Interop.swconst;
using System.Xml.Linq;
using SW_Console_Controller_V1.Models;
using System.Security.Cryptography;

namespace SW_Console_Controller_V1.Controllers
{
    internal class DrawingController : ModelController
    {
        public DrawingDoc Drawing;
        public SelectionMgr SelectionMgr;
        public ToleranceSheetProcessor ToleranceProcessor;
        public DataTable ToleranceData;
        private string[] DisabledViews;
        private string[] EnabledViews;
        private View[] Views;
        private Sheet Sheet;
        public DrawingController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, DrawingDoc drawing, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            Drawing = drawing;
            Sheet = Drawing.Sheet["NORMAL"];
            SelectionMgr = SwModel.SelectionManager;
            // for some reason casting this to View[] is not possible, despite the elements being Views
            object[] viewsTemp = Sheet.GetViews();
            Views = Array.ConvertAll(viewsTemp, v => (View)v);
            ToleranceProcessor = new ToleranceSheetProcessor(properties);
            if (Properties.ToolSeriesFileName != "" && Properties.ToolSeriesInputRange != "" && Properties.ToolSeriesOutputRange != "") ToleranceData = ToleranceProcessor.GetToleranceData();

            DrawingDimensionTools.LoadDimensionData();

            bool centerInPart = Properties.Center.UpperCenter || Properties.Center.LowerCenter;
            AddCenters(centerInPart);

            (EnabledViews, DisabledViews) = DrawingDimensionTools.GetViews();
            HideUnusedViews();
            if (Properties.LeftHandSpiral) MirrorSideViews();
            DrawingDimensionTools.MarkDimensions();
            DrawingDimensionTools.AddDimensions();

            UpdateDrawing();
            if (ToleranceData != null && (Properties.Prp.DrawingType == "Manufacturing" || (Properties.Prp.DrawingType == "Custom" && Properties.Prp.TableOnDrawing))) CreateTable();

            SwModel.ClearSelection2(true);
        }

        private void HideUnusedViews()
        {
            foreach (string disabledView in DisabledViews) Views.Where(v => v.GetName2() == disabledView).ToArray()[0].SetVisible(false, false);
        }

        private void MirrorSideViews()
        {
            // If left-hand spiral tool, all side views should be mirrored
            string[] sideViews = new string[] { "SIDE", "FORMING", "BLANK" };
            foreach (View view in Views)
            {
                if (sideViews.Contains(view.Name))
                {
                    SwModel.Extension.SelectByID2(view.Name, "DRAWINGVIEW", 0, 0, 0, false, 0, null, 0);
                    SwModel.ShowNamedView2(view.Name, (int)swStandardViews_e.swBackView);
                }
            }
        }

        private void UpdateDrawing()
        {
            if (Properties.Prp.DrawingType == "Custom") Sheet.SetTemplateName(Path.Combine(Properties.Prp.DrawingTypeFilePath, Properties.Prp.DrawingTypeFilename));
            else Sheet.SetTemplateName(Path.Combine(Properties.MasterPath, "drawing formats", $"{Properties.Prp.DrawingType}.slddrt"));
            Sheet.ReloadTemplate(true);

            if (ToleranceData != null) SetTolerances(Views);

            // auto align dimensions. TODO: make better spacing algorithm
            SwModel.Extension.SelectAll();
            SwModel.Extension.AlignDimensions(0, -0.1);
        }

        private void AddCenters(bool centerInPart)
        {
            if (!centerInPart)
            {
                DrawingDimensionTools.DrawingSelect("Detail View A (2 : 1)", "DRAWINGVIEW");
                DrawingDimensionTools.DrawingSelect("Detail View B (2 : 1)", "DRAWINGVIEW", true);
                DrawingDimensionTools.DrawingSelect("Detail View C (2 : 1)", "DRAWINGVIEW", true);
                SwModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                return;
            }

            foreach (View view in Views)
            {
                if (view.Type == (int)swDrawingViewTypes_e.swDrawingDetailView)
                {
                    string index = view.Name.Split(' ')[2];
                    double radius;
                    // A: bottom, B: top, C: top boss
                    switch (index)
                    {
                        case "A":
                            if (!Properties.Center.LowerCenter || !Properties.Center.LowerCenterOnDrawing)
                            {
                                DrawingDimensionTools.DrawingSelect(view.Name, "DRAWINGVIEW");
                                SwModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                                continue;
                            }
                            radius = Math.Max(decimal.ToDouble(Properties.Center.LowerCenterDimensions.D2Max), decimal.ToDouble(Properties.Center.LowerCenterDimensions.LMax * 2m));
                            break;
                        case "B":
                            if (!Properties.Center.UpperCenter || Properties.Center.UpperBoss || !Properties.Center.UpperCenterOnDrawing)
                            {
                                DrawingDimensionTools.DrawingSelect(view.Name, "DRAWINGVIEW");
                                SwModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                                continue;
                            }
                            radius = Math.Max(decimal.ToDouble(Properties.Center.UpperCenterDimensions.D2Max), decimal.ToDouble(Properties.Center.UpperCenterDimensions.LMax * 2m));
                            break;
                        case "C":
                            if (!Properties.Center.UpperBoss || !Properties.Center.UpperCenter || !Properties.Center.UpperCenterOnDrawing)
                            {
                                DrawingDimensionTools.DrawingSelect(view.Name, "DRAWINGVIEW");
                                SwModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                                continue;
                            }
                            radius = Math.Max(decimal.ToDouble(Properties.Center.UpperCenterDimensions.BossDiameterMax), decimal.ToDouble(Properties.Center.UpperCenterDimensions.BossLengthMax));
                            break;
                        default:
                            continue;
                    }
                    radius = radius / 2 * 1.2;
                    DetailCircle circle = view.GetDetail();
                    circle.SetParameters(0, 0, radius.ConvertToMeters());
                    SwModel.EditRebuild3();

                    AddCenterTolerances(index);
                }
            }
        }

        private void AddCenterTolerances(string type)
        {
            string[] dimensionNames; // names in drawing
            string[] propertyNames; // names in CenterDimensions

            if (type == "C")
            {
                dimensionNames = new string[] { "RD2", "RD1", "RD4", "RD3", "RD5", "RD6", "RD7" };
                propertyNames = new string[] { "A1", "A2", "D1", "D2", "L", "BossDiameter", "BossLength" };
            }
            else
            {
                dimensionNames = new string[] { "RD2", "RD1", "RD4", "RD3", "RD5" };
                propertyNames = new string[] { "A1", "A2", "D1", "D2", "L" };
            }

            CenterDimensions dimensions;

            if (type == "A") dimensions = Properties.Center.LowerCenterDimensions;
            else dimensions = Properties.Center.UpperCenterDimensions;

            for (int i = 0; i < dimensionNames.Length; i++)
            {
                DrawingDimensionTools.DrawingSelect($"{dimensionNames[i]}@Detail View {type} (2 : 1)", "DIMENSION");
                DisplayDimension displayDim = (DisplayDimension)SelectionMgr.GetSelectedObject6(1, -1);

                Annotation annotation = displayDim.GetAnnotation();
                if (annotation.IsDangling())
                {
                    SwModel.Extension.DeleteSelection2(1);
                    continue;
                }

                decimal minVal = (decimal)dimensions.GetType().GetProperty($"{propertyNames[i]}Min").GetValue(dimensions);
                decimal maxVal = (decimal)dimensions.GetType().GetProperty($"{propertyNames[i]}Max").GetValue(dimensions);
                if (minVal == maxVal) continue;

                Dimension dim = displayDim.GetDimension2(0);

                decimal dimensionValue = (decimal)dim.GetValue3(1, "")[0];
                minVal -= dimensionValue;
                maxVal -= dimensionValue;

                if (propertyNames[i].Substring(0, 1) == "A")
                {
                    // val is angle
                    minVal = (decimal)(decimal.ToDouble(minVal) * Math.PI / 180);
                    maxVal = (decimal)(decimal.ToDouble(maxVal) * Math.PI / 180);
                } else
                {
                    minVal = minVal.ConvertToMeters();
                    maxVal = maxVal.ConvertToMeters();
                }

                DimensionTolerance tolerance = dim.Tolerance;
                tolerance.Type = 3; // LIMIT
                tolerance.SetValues(decimal.ToDouble(minVal), decimal.ToDouble(maxVal));
            }
        }

        private void SetTolerances(View[] views)
        {
            for (int i = 0; i < views.Length; i++)
            {
                View view = views[i];
                object[] dimensionsTemp = view.GetDisplayDimensions();
                if (dimensionsTemp == null || dimensionsTemp.Length == 0) continue;
                // Same thing as with the views array, cannot cast the array directly
                DisplayDimension[] viewDimensions = Array.ConvertAll(dimensionsTemp, d => (DisplayDimension)d);

                for (int dim = 0; dim < viewDimensions.Length; dim++)
                {
                    // Find dimension in sheet
                    Annotation annotation = viewDimensions[dim].GetAnnotation();
                    string annotationName = annotation.GetName();
                    // for diameter reference dimensions: remove the ref in the name so it can be looked up
                    if (annotationName.EndsWith("_REF")) annotationName = annotationName.Replace("_REF", "");

                    // Tolerance dimension
                    DataRow[] toleranceData = ToleranceData.Select($"SOFTWARE_NAME = '{annotationName}'");
                    if (toleranceData.Length != 0) DrawingControllerTools.SetDimensionTolerance(viewDimensions[dim], toleranceData[0]);
                }
            }
        }

        private void CreateTable()
        {
            int maxRows = 20;
            DataRow[] toleranceData = ToleranceData.Select("TABLE_TYPE = 'VALUE' OR TABLE_TYPE = 'TEXT'");
            int count = toleranceData.Length;
            if (count == 0) return;
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
                if ((string)data["TABLE_TYPE"] == "TEXT")
                    cellText = (string)data["TABLE_TEXT"];
                else
                    cellText = $"{(string)data["NAME"]}: {(string)data["MIN_VAL"]} - {(string)data["MAX_VAL"]}";

                int rowIndex = i % rows;
                int colIndex = (int)Math.Floor((float)i / rows);

                table.Text[rowIndex, colIndex] = cellText;
            }
        }
    }
}
