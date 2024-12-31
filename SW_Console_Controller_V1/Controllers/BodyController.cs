using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class BodyController : ModelController
    {
        ModelController _toolController;
        public BodyController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel();
        }

        private void UpdateModel()
        {
            if (Properties.StraightFlute)
            {
                // Add default helix angle in case HelixAngle = null for dependencies (e.g. chipbreaker helix)
                Properties.HelixAngle = 40;
            }
            GeneratedProperties.HelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(Properties.HelixAngle * Math.PI / 180f);
            EquationController.SetEquation("FluteHelixPitch", GeneratedProperties.HelixPitch);
            EquationController.SetEquation("FluteCount", Properties.FluteCount);
            EquationController.SetEquation("FluteHelixAngle", Properties.HelixAngle);

            // TODO: check if this should happen for all tool types
            CenterController centerController = new CenterController(Properties, GeneratedProperties, SwModel, EquationManager);
            centerController.CreateCenterHoles();

            switch (Properties.ToolType)
            {
                case "End Mill":
                    _toolController = new EMController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Drill":
                    _toolController = new DrillController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Reamer":
                    _toolController = new ReamerController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Blank":
                    _toolController = new BlankController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
            }

            SwModel.ShowConfiguration2("Default");

            if (Properties.StepTool)
            {
                StepController stepController = new StepController(Properties, GeneratedProperties, SwModel, EquationManager);
                stepController.CreateSteps();
            }
        }
    }
}
