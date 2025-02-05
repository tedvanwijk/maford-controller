using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class ReamerController : ModelController
    {
        public ReamerController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel();
            if (Properties.Coolant.CoolantHole)
            {
                // TODO: enable CoolantPatternAlongFluting here as it is always going to be the case for reamers if not straight flute
                CoolantController coolantController = new CoolantController(Properties, GeneratedProperties, SwModel, EquationManager);
                coolantController.CreateCoolantHoles();
            }
            UpdateBlankConfiguration();
        }

        private void UpdateModel()
        {
            EquationController.SetEquation("ReamerFluteAngle", 360 / Properties.FluteCount - 25);

            decimal fluteDepth = 0.25m * Properties.ToolDiameter;
            EquationController.SetEquation("ReamerFluteDepth", fluteDepth);
            EquationController.SetEquation("ReamerFluteRadius", 0.1m * fluteDepth);

            if (Properties.StraightFlute) CreateStraightFluting(fluteDepth);
            else CreateFluting();

            switch (Properties.CornerStyle)
            {
                case "Corner Chamfer":
                    EquationController.SetEquation("ReamerCornerChamferWidth", Properties.CornerChamferWidth);
                    EquationController.SetEquation("ReamerCornerChamferAngle", Properties.CornerChamferAngle);
                    ModelControllerTools.UnsuppressFeature("REAMER_CORNER_CHAMFER_CUT");
                    break;
                case "Corner Radius":
                    EquationController.SetEquation("ReamerCornerRadius", Properties.CornerRadius);
                    ModelControllerTools.UnsuppressFeature("REAMER_CORNER_RADIUS_CUT");
                    break;
                case "Ballnose":
                    ModelControllerTools.UnsuppressFeature("REAMER_BALL_NOSE_CUT");
                    break;
            }
        }

        private void CreateStraightFluting(decimal fluteDepth)
        {
            ModelControllerTools.UnsuppressFeature("REAMER_STRAIGHT_FLUTE_PATTERN");

            //decimal bottomOffset = Properties.ToolDiameter + fluteDepth;
            //EquationController.SetEquation("ReamerStraightFluteBottomOffset", bottomOffset);
            //EquationController.SetEquation("ReamerStraightFluteBottomLength", bottomOffset);

            // Read the opening angle of the fluting
            double phi = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "phi", true);

            double deepestFluteDepth;
            if (phi > (Math.PI / 2))
            {
                // If the opening angle is more than 90 degrees, we need to calculcate the depth of the deepest point in the fluting
                double h_0 = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "h_0");
                double x_0 = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "x_0");
                double alpha = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "alpha", true);
                double R = decimal.ToDouble(Properties.ToolDiameter / 2);

                deepestFluteDepth = h_0 * (1 / Math.Tan(phi)) + R * (-Math.Cos(alpha) + Math.Sin(phi) + Math.Cos(phi) * Math.Cos(phi) / Math.Sin(phi)) + x_0;
            } else
            {
                double x_0 = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "x_0");

                deepestFluteDepth = x_0 + decimal.ToDouble(0.1m * fluteDepth) * (1 - Math.Sin(phi));
            }

            // If the shank is normal or reduced the fluting will never go into the shank, so we can simply set the bottom offset to the depth
            if (Properties.ShankType == "Normal" || Properties.ShankType == "Reduced" || (Properties.ShankType == "Neck" && Properties.ToolDiameter >= Properties.ShankDiameter))
            {
                EquationController.SetEquation("ReamerStraightFluteBottomOffset", deepestFluteDepth);
                return;
            }

            // Calculate available length in the body for the washout
            double washoutLengthInBody = decimal.ToDouble(GeneratedProperties.BodyLength - Properties.LOC);
            if (Properties.ShankType == "Neck") washoutLengthInBody += decimal.ToDouble(Properties.ShankNeckLength - GeneratedProperties.BodyLength);

            // Calculate the necessary length for the flute depth
            double washoutCurveAngle = 45;
            EquationController.SetEquation("ReamerWashoutCurveAngle", washoutCurveAngle);
            washoutCurveAngle = washoutCurveAngle.ConvertToRad();
            double deepestFluteDepthLength = deepestFluteDepth * Math.Sin(washoutCurveAngle) / (1 - Math.Cos(washoutCurveAngle));

            // If the available length is less than the required length, we add an additional offset
            if (deepestFluteDepthLength > washoutLengthInBody) deepestFluteDepth += decimal.ToDouble((Properties.ShankDiameter - Properties.ToolDiameter) / 2m);

            EquationController.SetEquation("ReamerStraightFluteBottomOffset", deepestFluteDepth);
        }

        private void CreateFluting()
        {
            ModelControllerTools.UnsuppressFeature("REAMER_FLUTE_PATTERN");

            double washoutDepth = Math.Tan(Properties.HelixAngle.ConvertToRad()) * decimal.ToDouble(Properties.LOA - Properties.LOC);

            // If the shank is normal or reduced the fluting will never go into the shank, so we can simply set the bottom offset to the depth
            if (Properties.ShankType == "Normal" || Properties.ShankType == "Reduced" || (Properties.ShankType == "Neck" && Properties.ToolDiameter >= Properties.ShankDiameter))
            {
                EquationController.SetEquation("ReamerFluteBottomOffset", washoutDepth);
                return;
            }

            double phi = ModelControllerTools.GetSketchDimension("REAMER_STRAIGHT_FLUTE_PROFILE_SKETCH", "phi", true);

            double fluteDepth;
            if (phi > (Math.PI / 2)) fluteDepth = ModelControllerTools.GetSketchDimension("REAMER_FLUTE_WASHOUT_PROFILE_SKETCH", "d_large");
            else fluteDepth = ModelControllerTools.GetSketchDimension("REAMER_FLUTE_WASHOUT_PROFILE_SKETCH", "d_small");

            // Calculate available length in the body for the washout
            double washoutLengthInBody = decimal.ToDouble(GeneratedProperties.BodyLength - Properties.LOC);
            if (Properties.ShankType == "Neck") washoutLengthInBody += decimal.ToDouble(Properties.ShankNeckLength - GeneratedProperties.BodyLength);

            // Calculate the necessary length for the flute depth
            double fluteDepthLength = fluteDepth / Math.Tan(Properties.HelixAngle.ConvertToRad());

            // If the available length is less than the required length, we add an additional offset
            if (fluteDepthLength <= washoutLengthInBody) washoutDepth = fluteDepth;

            EquationController.SetEquation("ReamerFluteBottomOffset", washoutDepth);
        }

        private void UpdateBlankConfiguration()
        {
            // select blank config, then suppress entire Reamer folder
            SwModel.ShowConfiguration2("Blank");
            SwModel.ClearSelection2(true);
            SwModel.Extension.SelectByID2("REAMER", "FTRFOLDER", 0, 0, 0, false, 0, null, 0);
            SwModel.EditSuppress2();
        }
    }
}
