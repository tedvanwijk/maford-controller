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
            EquationController.SetEquation("ReamerFluteRadius", 0.05m * Properties.ToolDiameter);

            decimal fluteDepth = 0.25m * Properties.ToolDiameter;
            EquationController.SetEquation("ReamerFluteDepth", fluteDepth);

            if (Properties.StraightFlute) CreateStraightFluting(fluteDepth);
            else CreateFluting();

            if (Properties.CornerStyle == "Corner Chamfer")
            {
                EquationController.SetEquation("ReamerCornerChamferWidth", Properties.CornerChamferWidth);
                EquationController.SetEquation("ReamerCornerChamferAngle", Properties.CornerChamferAngle);
                ModelControllerTools.UnsuppressFeature("REAMER_CORNER_CHAMFER_CUT");
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
            EquationController.SetEquation("ReamerStraightFluteBottomLength", 0.1m * Properties.LOC);

            ModelControllerTools.UnsuppressFeature("REAMER_STRAIGHT_FLUTE_PATTERN");
        }

        private void CreateFluting()
        {
            // TODO: test potential multiplication factor for helixpitch or larger helix diameter in part file
            EquationController.SetEquation("ReamerFluteWashoutHelixPitch", GeneratedProperties.HelixPitch);

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
