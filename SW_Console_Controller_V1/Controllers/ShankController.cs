using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SW_Console_Controller_V1.Lib;

namespace SW_Console_Controller_V1.Controllers
{
    internal class ShankController : ModelController
    {
        public ShankController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel(); 
        }

        private void UpdateModel()
        {
            GeneratedProperties.ShankChamferWidth = Properties.ToolDiameter < 0.2501m ? 0.03m : 0.045m;
            GeneratedProperties.ShankChamferAngle = Properties.ToolDiameter < 0.2501m ? 30m : 45m;
            GeneratedProperties.ShankToHeadRadius = Properties.ShankEndAtHead ? (Properties.ToolDiameter - (Properties.ShankType == "Neck" ? Properties.ShankNeckDiameter : Properties.ShankDiameter)) / 2 : Properties.ShankToHeadRadius;

            EquationController.SetEquation("ShankChamferAngle", $"{GeneratedProperties.ShankChamferAngle}in");
            EquationController.SetEquation("ShankChamferWidth", $"{GeneratedProperties.ShankChamferWidth}in");
            EquationController.SetEquation("ShankBlendAngle", $"{Properties.ShankBlendAngle}deg");
            EquationController.SetEquation("ShankToHeadRadius", $"{GeneratedProperties.ShankToHeadRadius}in");
            EquationController.SetEquation("ShankNeckLength", $"{Properties.ShankNeckLength}in");
            EquationController.SetEquation("ShankNeckDiameter", $"{Properties.ShankNeckDiameter}in");

            switch (Properties.ShankType)
            {
                case "Reduced":
                    ModelControllerTools.UnsuppressFeature("REDUCED_SHANK_CUT");
                    break;
                case "Neck":
                    ModelControllerTools.UnsuppressFeature("NECK_SHANK_CUT");
                    break;
                case "Blend":
                    ModelControllerTools.UnsuppressFeature("BLEND_SHANK_CUT");
                    break;
                case "Normal":
                    ModelControllerTools.UnsuppressFeature("SHANK_CUT");
                    break;
            }
        }
    }
}
