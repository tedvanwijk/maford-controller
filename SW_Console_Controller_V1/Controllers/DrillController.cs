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
            double fluteAngleRad = fluteAngle / 180 * Math.PI;
            double d = decimal.ToDouble(Properties.ToolDiameter / 2);
            double R0 = d;
            // TODO: implement the profile cut depth as a function of TD instead of a constant 0.01"
            double R1 = d - 0.01;

            double maxFluteDepthAngle = 30;
            maxFluteDepthAngle = maxFluteDepthAngle / 180 * Math.PI;
            double fluteDepth = R0 * (1 - 1 / Math.Tan(fluteAngleRad)); // x
            double maxFluteDepthDifference = fluteDepth / 2 * (1 - Math.Cos(maxFluteDepthAngle)); // Delta
            double maxFluteDepthAngleR2 = Math.Atan2(fluteDepth / 2 * Math.Sin(maxFluteDepthAngle), 2 * R0 - fluteDepth + maxFluteDepthDifference); // alpha
            double R2 = Math.Sqrt(Math.Pow(fluteDepth / 2 * (1 - Math.Cos(maxFluteDepthAngle)) + (1 + 1 / Math.Tan(fluteAngleRad)) * R0, 2) + Math.Pow(fluteDepth / 2 * Math.Sin(maxFluteDepthAngle), 2));

            double washoutHeightFactor = (Math.Pow(d, 2) + Math.Pow(R1, 2) - Math.Pow(R2, 2)) / (2 * d);
            double washoutHeightNumerator = Math.Sqrt(Math.Pow(R1, 2) - Math.Pow(washoutHeightFactor, 2));
            double washoutAngle = Math.Atan(washoutHeightNumerator / washoutHeightFactor);
            washoutAngle = Math.Asin(R0 * Math.Sin(washoutAngle) / R2) + maxFluteDepthAngleR2;
            decimal washoutHeight = (decimal)(2 * GeneratedProperties.HelixPitch * washoutAngle / (2 * Math.PI));
            // redefine LOC. Drills are defined with LOF and the LOC is calculated based on the flute depth and washout angle
            Properties.LOC = Properties.LOF - washoutHeight;
            EquationController.SetEquation("LOC", $"{Properties.LOC}in");

            // Set the end parameter for the washout curve so the washout doesn't cut into the shank (parameters don't support SW equations)
            SwModel.Extension.SelectByID2("DRILL_FLUTE_HELIX_WASHOUT_SKETCH", "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SwModel.SelectionManager.GetSelectedObject6(1, -1);
            Sketch sketch = (Sketch)feature.GetSpecificFeature2();
            var segments = sketch.GetSketchSegments();
            SketchSpline fluteWashoutCurve = (SketchSpline)segments[0];
            string xeq;
            string yeq;
            string zeq;
            fluteWashoutCurve.GetEquationParameters2(out xeq, out yeq, out zeq, out _, out _, out _, out _, out _, out _, out _, out _);
            fluteWashoutCurve.SetEquationParameters2(
                xeq,
                yeq,
                zeq,
                0,
                washoutAngle,
                false,
                0,
                0,
                0,
                true,
                true);
            SwModel.ClearSelection2(true);

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
