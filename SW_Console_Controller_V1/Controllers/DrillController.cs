using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SW_Console_Controller_V1.Controllers
{
    internal class DrillController : ModelController
    {
        public DrillController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel();
            UpdateBlankConfiguration();
        }

        private void UpdateModel()
        {
            decimal pointHeight = (decimal)((Properties.ToolDiameter / 2m) / (decimal)Math.Tan((decimal.ToDouble(Properties.PointAngle) / 2) / 180 * Math.PI));
            GeneratedProperties.PointHeight = pointHeight;
            if (!Properties.LOFFromPoint)
            {
                Properties.LOF = Properties.LOF + pointHeight;
                if (Properties.BodyLengthSameAsLOF)
                {
                    // Since LOF has been changed we also need to check if BodyLength = LOF and reset that as well
                    GeneratedProperties.BodyLength = Properties.LOF;
                    EquationController.SetEquation("BodyLength", $"{GeneratedProperties.BodyLength}in");
                }
                EquationController.SetEquation("LOF", $"{Properties.LOF}in");
            }

            if (!Properties.StraightFlute) CreateFluting(pointHeight);
            else CreateStraightFluting();

            if (Properties.StepTool)
            {
                StepController stepController = new StepController(Properties, GeneratedProperties, SwModel, EquationManager);
                stepController.CreateSteps();
            }
        }

        private void CreateFluting(decimal pointHeight)
        {
            double fluteAngle = 85;
            double fluteAngleRad = fluteAngle / 180 * Math.PI;
            double d = decimal.ToDouble(Properties.ToolDiameter / 2);
            double R0 = d;
            // TODO: implement the profile cut depth as a function of TD instead of a constant 0.01"
            //double R1 = d - 0.01;
            double R1 = d;

            double maxFluteDepthAngle = 45;
            maxFluteDepthAngle = maxFluteDepthAngle / 180 * Math.PI;
            double fluteDepth = R0 * (1 - 1 / Math.Tan(fluteAngleRad)); // x
            double maxFluteDepthDifference = fluteDepth / 2 * (1 - Math.Cos(maxFluteDepthAngle)); // Delta
            double maxFluteDepthAngleY = fluteDepth / 2 * Math.Sin(maxFluteDepthAngle);
            double maxFluteDepthAngleX = 2 * R0 - fluteDepth + maxFluteDepthDifference;
            double maxFluteDepthAngleR2 = Math.Atan2(maxFluteDepthAngleY, maxFluteDepthAngleX); // alpha
            double maxFluteDepthAngleR2Degrees = maxFluteDepthAngleR2 * 180 / Math.PI + 270;
            //double R2 = Math.Sqrt(Math.Pow(fluteDepth / 2 * (1 - Math.Cos(maxFluteDepthAngle)) + (1 + 1 / Math.Tan(fluteAngleRad)) * R0, 2) + Math.Pow(fluteDepth / 2 * Math.Sin(maxFluteDepthAngle), 2));
            double R2 = Math.Sqrt(Math.Pow(maxFluteDepthAngleY, 2) + Math.Pow(maxFluteDepthAngleX, 2));

            double washoutHeightFactor = (Math.Pow(d, 2) + Math.Pow(R1, 2) - Math.Pow(R2, 2)) / (2 * d);
            double washoutHeightNumerator = Math.Sqrt(Math.Pow(R1, 2) - Math.Pow(washoutHeightFactor, 2));
            double washoutAngle = Math.Atan(washoutHeightNumerator / washoutHeightFactor);
            washoutAngle = Math.Asin(R1 * Math.Sin(washoutAngle) / R2) + maxFluteDepthAngleR2;
            decimal washoutHeight = (decimal)(GeneratedProperties.HelixPitch * washoutAngle / (2 * Math.PI));
            // redefine LOC. Drills are defined with LOF and the LOC is calculated based on the flute depth and washout angle
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", $"{Properties.LOC}in");

            EquationController.SetEquation("DrillPointAngle", $"{Properties.PointAngle}in");

            EquationController.SetEquation("DrillWashoutHelixGuideDiameter", $"{R2 * 2}in");
            EquationController.SetEquation("DrillWashoutHelixGuideStartingAngle", $"{(int)Math.Round(maxFluteDepthAngleR2Degrees)}deg");

            double pointToLOCRotation = ((decimal.ToDouble(Properties.LOC) - decimal.ToDouble(pointHeight)) / GeneratedProperties.HelixPitch * 360) % 360;
            EquationController.SetEquation("DrillLOCToPointRotation", $"{pointToLOCRotation}");
            EquationController.SetEquation("DrillPointHeight", $"{pointHeight}in");

            ModelControllerTools.UnsuppressFeature("DRILL_POINT_ANGLE_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_WASHOUT_PATTERN");
            ModelControllerTools.UnsuppressFeature("DRILL_FLUTE_PATTERN");

            if (Properties.CoolantThrough)
            {
                ModelControllerTools.UnsuppressFeature("DRILL_COOLANT_PATTERN");
                double coolantAngle = 22.5;
                EquationController.SetEquation("DrillCoolantAngle", coolantAngle.ToString());
                double coolantRotation = (decimal.ToDouble(Properties.LOA - Properties.LOC - 2 * 0.08m * Properties.ToolDiameter) / GeneratedProperties.HelixPitch * 360 + coolantAngle) % 360;
                EquationController.SetEquation("DrillCoolantExitAngle", coolantRotation.ToString());
                ModelControllerTools.UnsuppressFeature("DRILL_COOLANT_SLOT_CUT");
            }
        }

        private void CreateStraightFluting()
        {
            decimal washoutHeight = 0.4m * Properties.LOF;
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", $"{Properties.LOC}in");

            // Profile depth calculation
            double smallestStepDiameter;
            if (Properties.StepTool) smallestStepDiameter = decimal.ToDouble(Properties.Steps[0].Diameter);
            else smallestStepDiameter = decimal.ToDouble(Properties.ToolDiameter);

            double offset = 0.07 * smallestStepDiameter;
            double insideRadius = 0.1742160 * smallestStepDiameter;
            double insideAngle = 100;
            insideAngle = insideAngle.ConvertToRad();
            double radius = decimal.ToDouble(Properties.ToolDiameter / 2);

            double delta = offset + insideRadius * (1 - Math.Cos(Math.PI - insideAngle));
            double depth = radius * Math.Sin(Math.Acos(-1 / radius * delta)) - offset / Math.Cos(Math.Abs(Math.PI / 2 - insideAngle)) + Math.Tan(Math.Abs(90 - insideAngle)) * delta;

            EquationController.SetEquation("DrillStraightFluteProfileDepth", $"{depth}in");

            ModelControllerTools.UnsuppressFeature("DRILL_POINT_ANGLE_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_STRAIGHT_FLUTE_PATTERN");
        }

        private void UpdateBlankConfiguration()
        {
            // select blank config, then suppress entire Drill folder
            SwModel.ShowConfiguration2("Blank");
            SwModel.ClearSelection2(true);
            SwModel.Extension.SelectByID2("DRILL", "FTRFOLDER", 0, 0, 0, false, 0, null, 0);
            SwModel.EditSuppress2();
            // point angle cut is part of the forming drawing but also in drill folder
            ModelControllerTools.UnsuppressFeature("DRILL_POINT_ANGLE_CUT");
        }
    }
}
