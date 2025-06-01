using Newtonsoft.Json.Bson;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SW_Console_Controller_V1.Lib;
using SW_Console_Controller_V1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class StepController : ModelController
    {
        public StepController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
        }
        public void CreateSteps()
        {
            for (int i = 0; i < Properties.Steps.Length; i++)
            {
                Step currentStep = Properties.Steps[i];
                decimal outerDiameter;

                // if last step, outerdiameter is equal to the tool diameter
                if (i == Properties.Steps.Length - 1) outerDiameter = Properties.ToolDiameter;
                else outerDiameter = Properties.Steps[i + 1].Diameter;

                decimal length = currentStep.Length;

                // adjust length of step
                if (Properties.ToolType != "Drill") Properties.Steps[i].LOFFromPoint = true;
                bool lofFromPoint = currentStep.LOFFromPoint;
                bool midpoint = currentStep.Midpoint && currentStep.RTop != 0;
                decimal midpointLength = currentStep.RTop * (decimal)Math.Sin(decimal.ToDouble(currentStep.Angle / 4m) * Math.PI / 180f);

                if (!lofFromPoint) length += GeneratedProperties.PointHeight;
                if (midpoint) length -= midpointLength;

                string sketchName;
                string sourceSketchName;
                string[] dimensionNames;
                decimal[] dimensions;

                if (currentStep.RTop == 0 && currentStep.RBottom == 0)
                {
                    sourceSketchName = "STEP_SKETCH_ANGLE";
                    dimensionNames = new string[] { "Length", "Angle", "InnerDiameter", "OuterDiameter" };
                    dimensions = new decimal[]
                    {
                    length,
                    currentStep.Angle,
                    currentStep.Diameter,
                    outerDiameter
                    };
                }
                else if (currentStep.RTop == 0)
                {
                    sourceSketchName = "STEP_SKETCH_RADIUS_BOTTOM";
                    dimensionNames = new string[] { "Length", "Angle", "InnerDiameter", "OuterDiameter", "RBottom" };
                    dimensions = new decimal[]
                    {
                    length,
                    currentStep.Angle,
                    currentStep.Diameter,
                    outerDiameter,
                    currentStep.RBottom
                    };
                }
                else if (currentStep.RBottom == 0)
                {
                    sourceSketchName = "STEP_SKETCH_RADIUS_TOP";
                    dimensionNames = new string[] { "Length", "Angle", "InnerDiameter", "OuterDiameter", "RTop" };
                    dimensions = new decimal[]
                    {
                    length,
                    currentStep.Angle,
                    currentStep.Diameter,
                    outerDiameter,
                    currentStep.RTop
                    };
                }
                else
                {
                    sourceSketchName = "STEP_SKETCH_RADIUS";
                    dimensionNames = new string[] { "Length", "Angle", "InnerDiameter", "OuterDiameter", "RTop", "RBottom" };
                    dimensions = new decimal[]
                    {
                        length,
                        currentStep.Angle,
                        currentStep.Diameter,
                        outerDiameter,
                        currentStep.RTop,
                        currentStep.RBottom
                    };
                }

                ModelControllerTools.SetSketchDimension(sourceSketchName, dimensionNames, dimensions);

                ModelControllerTools.SelectFeature(sourceSketchName, "SKETCH");

                SwModel.EditCopy();
                ModelControllerTools.SelectFeature("Front Plane", "PLANE");
                SwModel.Paste();

                Feature sketchFeature = SwModel.Extension.GetLastFeatureAdded();
                sketchName = $"STEP_{i}_SKETCH";
                sketchFeature.Name = sketchName;
                sketchFeature.Select2(false, 0);

                // Reset sketch relations
                SwModel.EditSketch();

                ModelControllerTools.SelectFeature("LOA_REF_PLANE", "PLANE");
                ModelControllerTools.SelectFeature($"Line45@{sketchName}", "SKETCHSEGMENT", true);
                SwModel.SketchAddConstraints("sgCOLINEAR");

                ModelControllerTools.SelectFeature("MAX_D_OFFSET_REF_PLANE", "PLANE");
                ModelControllerTools.SelectFeature($"Line44@{sketchName}", "SKETCHSEGMENT", true);
                SwModel.SketchAddConstraints("sgCOLINEAR");

                ModelControllerTools.SelectFeature("Line1@LENGTH_REF", "EXTSKETCHSEGMENT");
                ModelControllerTools.SelectFeature($"Line46@{sketchName}", "SKETCHSEGMENT", true);
                SwModel.SketchAddConstraints("sgCOLINEAR");

                ModelControllerTools.SelectFeature("DRILL_POINT_END_PLANE", "PLANE");
                ModelControllerTools.SelectFeature($"Point77@{sketchName}", "SKETCHSEGMENT", true);
                SwModel.SketchAddConstraints("sgCOINCIDENT");

                SwModel.SketchManager.InsertSketch(false);

                // Select revolving axis and sketch and add feature
                ModelControllerTools.SelectFeature(sketchName, "SKETCH", false, 0);
                ModelControllerTools.SelectFeature("Line1@LENGTH_REF", "SKETCHSEGMENT", true, 4);
                Feature cut = SwModel.FeatureManager.FeatureRevolveCut2(
                    2 * Math.PI,
                    false,
                    0,
                    0,
                    0,
                    false,
                    true,
                    false,
                    false,
                    true
                    ) ?? throw new Exception($"Failed to create step {i + 1}. Most likely the step is larger than the step before or the tool diameter, which is not supported.");
                cut.Name = $"STEP_{i}_CUT";
                SwModel.Extension.ReorderFeature(cut.Name, "STEP", (int)swMoveLocation_e.swMoveToFolder);
            }
        }
    
        public void CreateStraightMargins(double insideAngle, double straightFluteOffset, double marginAngle)
        {
            List<string> stepFeatureNames = new List<string>();

            ModelControllerTools.UnsuppressFeature("STEP_SKETCH_MARGIN_STRAIGHT_FLUTE");
            ModelControllerTools.UnsuppressFeature("STEP_SKETCH_MARGIN_MIDDLE_STRAIGHT_FLUTE");

            for (int i = 0; i < Properties.Steps.Length; i++)
            {
                Step currentStep = Properties.Steps[i];

                if (!currentStep.FrontMargin && !currentStep.MiddleMargin && !currentStep.RearMargin) continue;

                decimal length = currentStep.Length;

                // adjust length of step
                bool lofFromPoint = currentStep.LOFFromPoint;
                bool midpoint = currentStep.Midpoint && currentStep.RTop != 0;
                decimal midpointLength = currentStep.RTop * (decimal)Math.Sin(decimal.ToDouble(currentStep.Angle / 4m) * Math.PI / 180f);

                if (!lofFromPoint) length += GeneratedProperties.PointHeight;
                if (midpoint) length -= midpointLength;

                decimal marginDepthFactor = 0.00625m;
                decimal marginDiameter = currentStep.Diameter * (1m - 2m * marginDepthFactor);

                string sourceSketchName;
                if (currentStep.MiddleMargin) sourceSketchName = "STEP_SKETCH_MARGIN_MIDDLE_STRAIGHT_FLUTE";
                else  sourceSketchName = "STEP_SKETCH_MARGIN_STRAIGHT_FLUTE";

                ModelControllerTools.SelectFeature(sourceSketchName, "SKETCH");

                SwModel.EditCopy();
                ModelControllerTools.SelectFeature("LOA_REF_PLANE", "PLANE");
                SwModel.Paste();

                Feature sketchFeature = SwModel.Extension.GetLastFeatureAdded();
                string sketchName = $"{sourceSketchName}_{i}";
                sketchFeature.Name = sketchName;

                string[] dimensionNames;
                decimal[] dimensions;

                double localInsideAngle = insideAngle - (2 * Math.Asin(2 * straightFluteOffset / decimal.ToDouble(currentStep.Diameter))).ConvertToDeg();

                localInsideAngle = 360f / Properties.FluteCount - localInsideAngle - marginAngle;

                if (currentStep.FrontMargin && currentStep.RearMargin)
                {
                    dimensionNames = new string[] { "FrontMarginAngle", "RearMarginAngle", "MarginDiameter", "StepDiameter", "InsideAngle" };
                    dimensions = new decimal[] { 0, 0, marginDiameter, currentStep.Diameter, (decimal)localInsideAngle };
                } else if (currentStep.FrontMargin && !currentStep.RearMargin)
                {
                    dimensionNames = new string[] { "FrontMarginAngle", "MarginDiameter", "StepDiameter", "InsideAngle" };
                    dimensions = new decimal[] { 0, marginDiameter, currentStep.Diameter, (decimal)localInsideAngle };
                } else if (!currentStep.FrontMargin && currentStep.RearMargin)
                {
                    dimensionNames = new string[] { "RearMarginAngle", "MarginDiameter", "StepDiameter", "InsideAngle" };
                    dimensions = new decimal[] { 0, marginDiameter, currentStep.Diameter, (decimal)localInsideAngle };
                }
                else
                {
                    dimensionNames = new string[] { "MarginDiameter", "StepDiameter", "InsideAngle" };
                    dimensions = new decimal[] { marginDiameter, currentStep.Diameter, (decimal)localInsideAngle };
                }

                ModelControllerTools.SetSketchDimension(sketchName, dimensionNames, dimensions);

                sketchFeature.Select2(false, 0);

                // Reset sketch relations
                SwModel.EditSketch();

                if (Properties.StraightFlute && currentStep.MiddleMargin)
                {
                    ModelControllerTools.SelectFeature("Point1@Origin", "EXTSKETCHPOINT");
                    ModelControllerTools.SelectFeature($"Point141@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgCOINCIDENT");

                    ModelControllerTools.SelectFeature("MAX_D_OFFSET_REF_PLANE", "PLANE");
                    ModelControllerTools.SelectFeature($"Arc6@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgTANGENT");

                    ModelControllerTools.SelectFeature("Line4@DRILL_STRAIGHT_FLUTE_ANGLE_REF_SKETCH", "EXTSKETCHSEGMENT");
                    ModelControllerTools.SelectFeature($"Point152@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgCOINCIDENT");
                }
                else
                {
                    ModelControllerTools.SelectFeature("Point1@Origin", "EXTSKETCHPOINT");
                    ModelControllerTools.SelectFeature($"Point131@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgCOINCIDENT");

                    ModelControllerTools.SelectFeature("MAX_D_OFFSET_REF_PLANE", "PLANE");
                    ModelControllerTools.SelectFeature($"Arc6@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgTANGENT");

                    ModelControllerTools.SelectFeature("Line4@DRILL_STRAIGHT_FLUTE_ANGLE_REF_SKETCH", "EXTSKETCHSEGMENT");
                    ModelControllerTools.SelectFeature($"Point145@{sketchName}", "SKETCHSEGMENT", true);
                    SwModel.SketchAddConstraints("sgCOINCIDENT");
                }

                SwModel.SketchManager.InsertSketch(false);

                ModelControllerTools.SelectFeature(sketchName, "SKETCH", false, 0);
                Feature cut = SwModel.FeatureManager.FeatureCut4(
                    true,                                       // single ended
                    false,                                      // don't flip side
                    false,                                      // don't flip direction
                    (int)swEndConditions_e.swEndCondBlind,      // blind cut
                    0,                                          // type for 2nd side
                    decimal.ToDouble(length.ConvertToMeters()), // length for 1st dir
                    0,                                          // length for 2nd dir
                    false,                                      // no draft
                    false,                                      // draft for 2nd side
                    false,                                      // draft direction 1
                    false,                                      // draft direction 2
                    0,                                          // draft angle 1
                    0,                                          // draft angle 2
                    false,                                      // OffsetReverse1
                    false,                                      // OffsetReverse2
                    false,                                      // TranslateSurface1
                    false,                                      // TranslateSurface2
                    false,                                      // NormalCut
                    false,                                      // affects all bodies
                    true,                                       // UseAutoSelect
                    false,                                      // AssemblyFeatureScope
                    true,                                       // AutoSelectComponents
                    true,                                       // PropagateFeatureToParts
                    (int)swStartConditions_e.swStartSketchPlane,// start condition
                    0,                                          // StartOffset
                    false,                                      // FlipStartOffset
                    false                                       // OptimizeGeometry
                    );
                cut.Name = $"STEP_{i}_MARGIN_CUT";
                stepFeatureNames.Add(cut.Name);
                SwModel.Extension.ReorderFeature(cut.Name, "STEP", (int)swMoveLocation_e.swMoveToFolder);
            }

            SwModel.ClearSelection2(true);

            // Select revolving axis
            ModelControllerTools.SelectFeature("Line1@LENGTH_REF", "EXTSKETCHSEGMENT", false, 1);

            // Select all margin cuts
            foreach (string featureName in stepFeatureNames) ModelControllerTools.SelectFeature(featureName, "BODYFEATURE", true, 4);
            
            Feature pattern = SwModel.FeatureManager.FeatureCircularPattern4(
                Properties.FluteCount,          // count
                2 * Math.PI,                    // total angle
                false,                          // FlipDirection
                "NULL",                         // DName
                false,                          // GeometryPattern
                true,                           // EqualSpacing
                false                           // VaryInstance
                );
            pattern.Name = "STEP_MARGIN_PATTERN";
            SwModel.Extension.ReorderFeature(pattern.Name, "STEP", (int)swMoveLocation_e.swMoveToFolder);
        }
    }
}
