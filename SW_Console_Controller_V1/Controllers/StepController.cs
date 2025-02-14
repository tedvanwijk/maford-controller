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
                    );
                cut.Name = $"STEP_{i}_CUT";
                SwModel.Extension.ReorderFeature(cut.Name, "STEP", (int)swMoveLocation_e.swMoveToFolder);
            }
        }
    }
}
