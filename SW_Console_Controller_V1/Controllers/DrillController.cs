using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            decimal pointHeight = (decimal)((GeneratedProperties.TopStepDiameter/ 2m) / (decimal)Math.Tan((decimal.ToDouble(Properties.PointAngle) / 2f) / 180f * Math.PI));
            GeneratedProperties.PointHeight = pointHeight;
            if (!Properties.LOFFromPoint)
            {
                Properties.LOF = Properties.LOF + pointHeight;
                if (Properties.BodyLengthSameAsLOF)
                {
                    // Since LOF has been changed we also need to check if BodyLength = LOF and reset that as well
                    GeneratedProperties.BodyLength = Properties.LOF;
                    EquationController.SetEquation("BodyLength", GeneratedProperties.BodyLength);
                }
                EquationController.SetEquation("LOF", Properties.LOF);
            }

            if (!Properties.StraightFlute) CreateFluting(pointHeight);
            else CreateStraightFluting(pointHeight);

            if (Properties.StepTool)
            {
                StepController stepController = new StepController(Properties, GeneratedProperties, SwModel, EquationManager);
                stepController.CreateSteps();
            }
        }

        private void CreateFluting(decimal pointHeight)
        {
            double fluteAngle;
            if (Properties.FluteCount == 2) fluteAngle = 85f;
            //else if (Properties.FluteCount == 3) fluteAngle = 55f;
            else throw new Exception($"Flute count = {Properties.FluteCount} for drill tool type not supported");
            double fluteAngleRad = fluteAngle / 180f * Math.PI;
            double d = decimal.ToDouble(Properties.ToolDiameter / 2m);
            double R0 = d;
            // TODO: implement the profile cut depth as a function of TD instead of a constant 0.01"
            //double R1 = d - 0.01;
            double R1 = d;

            double maxFluteDepthAngle = 45f;
            maxFluteDepthAngle = maxFluteDepthAngle / 180f * Math.PI;
            double fluteDepth = R0 * (1f - 1f / Math.Tan(fluteAngleRad)); // x
            double maxFluteDepthDifference = fluteDepth / 2f * (1f - Math.Cos(maxFluteDepthAngle)); // Delta
            double maxFluteDepthAngleY = fluteDepth / 2 * Math.Sin(maxFluteDepthAngle);
            double maxFluteDepthAngleX = 2f * R0 - fluteDepth + maxFluteDepthDifference;
            double maxFluteDepthAngleR2 = Math.Atan2(maxFluteDepthAngleY, maxFluteDepthAngleX); // alpha
            double maxFluteDepthAngleR2Degrees = maxFluteDepthAngleR2 * 180f / Math.PI + 270f;
            //double R2 = Math.Sqrt(Math.Pow(fluteDepth / 2 * (1 - Math.Cos(maxFluteDepthAngle)) + (1 + 1 / Math.Tan(fluteAngleRad)) * R0, 2) + Math.Pow(fluteDepth / 2 * Math.Sin(maxFluteDepthAngle), 2));
            double R2 = Math.Sqrt(Math.Pow(maxFluteDepthAngleY, 2f) + Math.Pow(maxFluteDepthAngleX, 2f));

            double washoutHeightFactor = (Math.Pow(d, 2f) + Math.Pow(R1, 2f) - Math.Pow(R2, 2f)) / (2f * d);
            double washoutHeightNumerator = Math.Sqrt(Math.Pow(R1, 2f) - Math.Pow(washoutHeightFactor, 2f));
            double washoutAngle = Math.Atan(washoutHeightNumerator / washoutHeightFactor);
            washoutAngle = Math.Asin(R1 * Math.Sin(washoutAngle) / R2) + maxFluteDepthAngleR2;
            decimal washoutHeight = (decimal)(GeneratedProperties.HelixPitch * washoutAngle / (2f * Math.PI));
            // redefine LOC. Drills are defined with LOF and the LOC is calculated based on the flute depth and washout angle
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", Properties.LOC);

            EquationController.SetEquation("DrillPointAngle", Properties.PointAngle);

            EquationController.SetEquation("DrillFluteAngle", fluteAngle);
            EquationController.SetEquation("DrillFluteCenterOffsetAngle", 0);
            EquationController.SetEquation("DrillFluteCenterOffset", Properties.ToolDiameter * 0.043578m);

            EquationController.SetEquation("DrillWashoutHelixGuideDiameter", R2 * 2f);
            EquationController.SetEquation("DrillWashoutHelixGuideStartingAngle", (int)Math.Round(maxFluteDepthAngleR2Degrees));

            double pointToLOCRotation = ((decimal.ToDouble(Properties.LOC) - decimal.ToDouble(pointHeight)) / GeneratedProperties.HelixPitch * 360f) % 360f;
            EquationController.SetEquation("DrillLOCToPointRotation", pointToLOCRotation);
            EquationController.SetEquation("DrillPointHeight", pointHeight);

            ModelControllerTools.UnsuppressFeature("DRILL_POINT_ANGLE_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_WASHOUT_PATTERN");
            ModelControllerTools.UnsuppressFeature("DRILL_FLUTE_PATTERN");

            double drillProfileAngle = 84f;
            EquationController.SetEquation("DrillProfileAngle", drillProfileAngle);
            EquationController.SetEquation("DrillProfileOpenAngle", 90f - drillProfileAngle);
            EquationController.SetEquation("DrillProfileDepth", 0.00625m * Properties.ToolDiameter);
            decimal drillProfileHelixHeight = Properties.LOF - 0.1m * (Properties.LOF - Properties.LOC);
            EquationController.SetEquation("DrillProfileHelixHeight", drillProfileHelixHeight);
            EquationController.SetEquation("DrillProfileHelixStartingAngle", 270f - decimal.ToDouble(drillProfileHelixHeight - Properties.LOC) / GeneratedProperties.HelixPitch * 360f);
            EquationController.SetEquation("DrillWashoutHelixPitch", GeneratedProperties.HelixPitch);
            EquationController.SetEquation("DrillWashoutHelixHeight", Properties.LOF - Properties.LOC);

            if (Properties.CoolantThrough)
            {
                decimal drillCoolantDiameter = 0.08m * GeneratedProperties.TopStepDiameter;
                decimal drillCoolantSlotWidth = 1.05m * drillCoolantDiameter;
                ModelControllerTools.UnsuppressFeature("DRILL_COOLANT_PATTERN");
                double coolantAngle = 22.5f;
                EquationController.SetEquation("DrillCoolantAngle", coolantAngle);
                double coolantRotation = (decimal.ToDouble(Properties.LOA - Properties.LOC - drillCoolantSlotWidth) / GeneratedProperties.HelixPitch * 360f + coolantAngle) % 360f;
                EquationController.SetEquation("DrillCoolantExitAngle", coolantRotation);
                EquationController.SetEquation("DrillCoolantDiameter", drillCoolantDiameter);
                EquationController.SetEquation("DrillCoolantOffset", 0.3125m * GeneratedProperties.TopStepDiameter);
                EquationController.SetEquation("DrillCoolantStartOffset", drillCoolantSlotWidth);
                EquationController.SetEquation("DrillCoolantHelixHeight", Properties.LOA - drillCoolantSlotWidth);
                EquationController.SetEquation("DrillCoolantSlotWidth", drillCoolantSlotWidth);
                ModelControllerTools.UnsuppressFeature("DRILL_COOLANT_SLOT_CUT");
            }
        }

        private void CreateStraightFluting(decimal pointHeight)
        {
            decimal washoutHeight = 0.4m * Properties.LOF;
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", Properties.LOC);
            EquationController.SetEquation("DrillPointAngle", Properties.PointAngle);
            EquationController.SetEquation("DrillPointHeight", pointHeight);

            // Profile depth calculation
            double smallestStepDiameter;
            if (Properties.StepTool) smallestStepDiameter = decimal.ToDouble(Properties.Steps[0].Diameter);
            else smallestStepDiameter = decimal.ToDouble(Properties.ToolDiameter);

            double offset = 0.07f * smallestStepDiameter;
            double insideRadius = 0.1742160f * smallestStepDiameter;
            double insideAngle = 100f;
            insideAngle = insideAngle.ConvertToRad();
            double radius = decimal.ToDouble(Properties.ToolDiameter / 2m);

            double delta = offset + insideRadius * (1f - Math.Cos(Math.PI - insideAngle));
            double depth = radius * Math.Sin(Math.Acos(-1f / radius * delta)) - offset / Math.Cos(Math.Abs(Math.PI / 2f - insideAngle)) + Math.Tan(Math.Abs(90f - insideAngle)) * delta;

            EquationController.SetEquation("DrillStraightFluteProfileDepth", depth);
            EquationController.SetEquation("DrillStraightFluteOffset", 0.07m * GeneratedProperties.TopStepDiameter);
            EquationController.SetEquation("DrillStraightFluteInsideRadius", 0.174216m * GeneratedProperties.TopStepDiameter);
            decimal washoutLength = 0.4m * Properties.LOF;
            EquationController.SetEquation("DrillStraightFluteWashoutLength", washoutLength);
            EquationController.SetEquation("DrillStraightFluteWashoutAngle", Math.Atan(depth / decimal.ToDouble(washoutLength)).ConvertToDeg() * 2f);

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
