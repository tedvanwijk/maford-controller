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

            EquationController.SetEquation("ShankChamferAngle", GeneratedProperties.ShankChamferAngle);
            EquationController.SetEquation("ShankChamferWidth", GeneratedProperties.ShankChamferWidth);
            EquationController.SetEquation("ShankBlendAngle", Properties.ShankBlendAngle);
            EquationController.SetEquation("ShankNeckLength", Properties.ShankNeckLength);
            EquationController.SetEquation("ShankNeckDiameter", Properties.ShankNeckDiameter);

            if (Properties.ShankEndAtHead)
            {
                // If this is the case, we need to calculate the necessary radius for the given diameters and angle
                decimal smallDiameter;
                if (Properties.ShankType == "Neck") smallDiameter = Properties.ShankNeckDiameter;
                else smallDiameter = Properties.ShankDiameter;

                GeneratedProperties.ShankToHeadRadius = (Properties.ToolDiameter - smallDiameter) / (2 * (1 - (decimal)Math.Cos(Properties.ShankToHeadAngle.ConvertToRad())));
            } else GeneratedProperties.ShankToHeadRadius = Properties.ShankToHeadRadius;

            // If the angle != 90, we need to add the angle to the drawing
            if (Properties.ShankToHeadAngle == 90) Properties.ShankToHeadAngleOnDrawing = false;
            else Properties.ShankToHeadAngleOnDrawing = true;

            EquationController.SetEquation("ShankToHeadRadius", GeneratedProperties.ShankToHeadRadius);
            EquationController.SetEquation("ShankToHeadAngle", Properties.ShankToHeadAngle);

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
