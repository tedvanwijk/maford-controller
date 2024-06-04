using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class EMController : ModelController
    {
        public EMController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel();
        }

        private void UpdateModel()
        {
            // Set profile dimensions
            double angle = 360 / decimal.ToDouble(Properties.FluteCount) * 0.844;
            double loftSketchAngle = ((decimal.ToDouble(Properties.LOC) / GeneratedProperties.HelixPitch) * 360 - (2 * angle / 3)) % 360;
            EquationController.SetEquation("EMLoftPlaneAngle", $"{loftSketchAngle}deg");

            // Set and enable pattern
            ModelControllerTools.UnsuppressFeature("EM_FLUTE_PATTERN");

            // Set corner style options
            EquationController.SetEquation("EMCornerRadius", $"{Properties.CornerRadius}in");
            EquationController.SetEquation("EMCornerChamferAngle", $"{Properties.CornerChamferAngle}deg");
            EquationController.SetEquation("EMCornerChamferWidth", $"{Properties.CornerChamferWidth}in");

            // Set loc rotation and helix angle
            EquationController.SetEquation("EMFluteLOCRotation", $"{(decimal.ToDouble(Properties.LOC) / GeneratedProperties.HelixPitch * 360) % 360}");
            EquationController.SetEquation("EMHelixAngle", $"{Properties.HelixAngle}deg");

            switch (Properties.CornerStyle)
            {
                case "Corner Chamfer":
                    ModelControllerTools.UnsuppressFeature("EM_CORNER_CHAMFER_CUT");
                    break;
                case "Corner Radius":
                    ModelControllerTools.UnsuppressFeature("EM_CORNER_RADIUS_CUT");
                    break;
                case "Ballnose":
                    ModelControllerTools.UnsuppressFeature("EM_BALL_NOSE_CUT");
                    break;
            }
        } 
    }
}
