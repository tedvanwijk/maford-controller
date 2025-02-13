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
            decimal pointHeight = (decimal)((GeneratedProperties.TopStepDiameter / 2m) / (decimal)Math.Tan((decimal.ToDouble(Properties.PointAngle) / 2f) / 180f * Math.PI));
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
        }

        private void CreateFluting(decimal pointHeight)
        {
            double fluteAngle = 85f;

            // Adjust flute based on flute count, throw exception if fc not supported
            double fluteOffsetAngle;
            if (Properties.FluteCount == 2) fluteOffsetAngle = 160;
            else if (Properties.FluteCount == 3) fluteOffsetAngle = 130;
            else throw new Exception($"Flute count = {Properties.FluteCount} for drill tool type not supported");

            EquationController.SetEquation("DrillFluteAngle", fluteAngle);
            EquationController.SetEquation("DrillFluteCenterOffsetAngle", fluteOffsetAngle);
            EquationController.SetEquation("DrillPointAngle", Properties.PointAngle);

            // Unsupress so driven dimensions can be read
            ModelControllerTools.UnsuppressFeature("DRILL_POINT_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_PROFILE_PATTERN");
            ModelControllerTools.UnsuppressFeature("DRILL_FLUTE_PATTERN");

            // Get flute depth from sketch and calculate heigth
            double fluteDepth = ModelControllerTools.GetSketchDimension("DRILL_FLUTE_PROFILE_SKETCH", "FluteProfileDepth");
            double washoutHeight = fluteDepth / Math.Tan(Properties.HelixAngle.ConvertToRad());

            // redefine LOC. Drills are defined with LOF and the LOC is calculated based on the flute depth and washout angle
            Properties.LOC = Properties.LOF - (decimal)washoutHeight;
            EquationController.SetEquation("LOC", Properties.LOC);

            double pointToLOCRotation = ((decimal.ToDouble(Properties.LOC) - decimal.ToDouble(pointHeight)) / GeneratedProperties.HelixPitch * 360f) % 360f;
            EquationController.SetEquation("DrillLOCToPointRotation", pointToLOCRotation);
            EquationController.SetEquation("DrillPointHeight", pointHeight);

            double drillProfileOpenAngle = 11f;
            double drillProfileAngle = 360 / Properties.FluteCount - fluteAngle - drillProfileOpenAngle;
            EquationController.SetEquation("DrillProfileAngle", drillProfileAngle);
            EquationController.SetEquation("DrillProfileDepth", 0.00625m * Properties.ToolDiameter);
            decimal drillProfileHelixHeight = Properties.LOF - 0.05m * (Properties.LOF - Properties.LOC);
            EquationController.SetEquation("DrillProfileHelixHeight", drillProfileHelixHeight);
            EquationController.SetEquation("DrillProfileHelixStartingAngle", 270f - decimal.ToDouble(drillProfileHelixHeight - Properties.LOC) / GeneratedProperties.HelixPitch * 360f);

            if (Properties.Coolant.CoolantHole)
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
            decimal washoutLength = 0.4m * Properties.LOF;
            Properties.LOC = Properties.LOF - washoutLength;
            EquationController.SetEquation("LOC", Properties.LOC);
            EquationController.SetEquation("DrillPointAngle", Properties.PointAngle);
            EquationController.SetEquation("DrillPointHeight", pointHeight);

            // Flute profile dimensioning
            double insideAngle;
            if (Properties.FluteCount == 2) insideAngle = 100f;
            else if (Properties.FluteCount == 3) insideAngle = 90f;
            else throw new Exception($"Flute count = {Properties.FluteCount} for drill tool type not supported");

            EquationController.SetEquation("DrillStraightFluteInsideAngle", insideAngle);
            EquationController.SetEquation("DrillStraightFluteOffset", 0.07m * GeneratedProperties.TopStepDiameter);
            EquationController.SetEquation("DrillStraightFluteInsideRadius", 0.174216m * GeneratedProperties.TopStepDiameter);

            ModelControllerTools.UnsuppressFeature("DRILL_POINT_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_STRAIGHT_FLUTE_PATTERN");

            // Drills will never have a body length < LOF, so checking if the fluting washout will run into the shank is not necessary
            // Instead, we just get the flute depth and calculate the washout angle and set it
            double fluteDepth = ModelControllerTools.GetSketchDimension("DRILL_STRAIGHT_FLUTE_LOC_PROFILE_SKETCH", "FluteProfileDepth");

            EquationController.SetEquation("DrillStraightFluteWashoutLength", washoutLength);
            EquationController.SetEquation("DrillStraightFluteWashoutAngle", Math.Atan(fluteDepth / decimal.ToDouble(washoutLength)).ConvertToDeg() * 2f);

            if (Properties.Coolant.CoolantHole)
            {
                CoolantController coolantController = new CoolantController(Properties, GeneratedProperties, SwModel, EquationManager);
                coolantController.CreateCoolantHoles();
            }
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
