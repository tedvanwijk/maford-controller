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
            if (!Properties.LOFFromPoint)
            {
                decimal pointHeight = (decimal)((Properties.ToolDiameter / 2m) / (decimal)Math.Tan((decimal.ToDouble(Properties.PointAngle) / 2) / 180 * Math.PI));
                Properties.LOF = Properties.LOF + pointHeight;
                if (Properties.BodyLengthSameAsLOF)
                {
                    // Since LOF has been changed we also need to check if BodyLength = LOF and reset that as well
                    GeneratedProperties.BodyLength = Properties.LOF;
                    EquationController.SetEquation("BodyLength", $"{GeneratedProperties.BodyLength}in");
                }
                EquationController.SetEquation("LOF", $"{Properties.LOF}in");
            }

            double fluteAngle = 85;
            decimal washoutHeight = (decimal)((GeneratedProperties.HelixPitch / 4) * (1 - (1 / Math.Tan(fluteAngle / 180 * Math.PI))));
            // redefine LOC. Drills are defined with LOF and the LOC is calculated based on the flute depth and washout angle
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", $"{Properties.LOC}in");

            EquationController.SetEquation("DrillPointAngle", $"{Properties.PointAngle}in");

            ModelControllerTools.UnsuppressFeature("DRILL_POINT_ANGLE_CUT");
            ModelControllerTools.UnsuppressFeature("DRILL_FLUTE_PATTERN");
        }

        private void UpdateBlankConfiguration()
        {
            // select blank config, then suppress entire Drill folder
            SwModel.ShowConfiguration2("Blank");
            SwModel.ClearSelection2(true);
            SwModel.Extension.SelectByID2("DRILL", "FTRFOLDER", 0, 0, 0, false, 0, null, 0);
            SwModel.EditSuppress2();
        }
    }
}
