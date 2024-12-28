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

            if (Properties.StepTool)
            {
                StepController stepController = new StepController(Properties, GeneratedProperties, SwModel, EquationManager);
                stepController.CreateSteps();
            }
        }

        private void CreateStraightFluting(decimal fluteDepth)
        {
            decimal bottomOffset = Properties.ToolDiameter + fluteDepth;
            EquationController.SetEquation("ReamerStraightFluteBottomOffset", bottomOffset);
            EquationController.SetEquation("ReamerStraightFluteBottomLength", bottomOffset);

            ModelControllerTools.UnsuppressFeature("REAMER_STRAIGHT_FLUTE_PATTERN");
        }

        private void CreateFluting()
        {
            decimal bottomOffset = Properties.LOA - Properties.LOC;
            EquationController.SetEquation("ReamerFluteBottomOffset", bottomOffset);

            ModelControllerTools.UnsuppressFeature("REAMER_FLUTE_PATTERN");
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
