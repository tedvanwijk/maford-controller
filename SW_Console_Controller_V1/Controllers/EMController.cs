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
        public EMController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model) : base(properties, generatedProperties, model)
        {
            UpdateModel();
        }

        private void UpdateModel()
        {
            CreateFlutes();
        } 

        private void CreateFlutes()
        {
            // Set up 3D washout curve
            double pitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(Properties.HelixAngle * Math.PI / 180);

            SwModel.Extension.SelectByID2("EM_FLUTE_HELIX_WASHOUT_SKETCH", "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SwModel.SelectionManager.GetSelectedObject6(1, -1);
            Sketch sketch = (Sketch)feature.GetSpecificFeature2();
            var segments = sketch.GetSketchSegments();
            SketchSpline fluteWashoutCurve = (SketchSpline)segments[0];
            string xEq = $"{Properties.ToolDiameter} * sin(t - {0.5 * Math.PI}) + {decimal.ToDouble(Properties.ToolDiameter) * 0.5}";
            string zEq = $"{Properties.ToolDiameter} * cos(t + {0.5 * Math.PI})";
            string yEq = $"({Properties.LOA} - {Properties.LOC}) - {pitch / (2 * Math.PI)} * 2 * t";
            fluteWashoutCurve.SetEquationParameters2(
                xEq.Replace(',', '.'),
                yEq.Replace(',', '.'),
                zEq.Replace(',', '.'),
                0,
                3.14,
                false,
                0,
                0,
                0,
                true,
                true);

            // TODO: currently it does not actually close the 3D sketch. Instead it just moves on and lets SW close it automatically. Maybe look for a way to close it.
            SwModel.ClearSelection2(true);

            // Set profile dimensions

            double angle = 360 / decimal.ToDouble(Properties.FluteCount) * 0.844;
            double loftAngle = 2/3 * angle;
            double leadingEdgeDepth = 0.1 * decimal.ToDouble(Properties.ToolDiameter);
            double trailingEdgeDepth = 0.1 * decimal.ToDouble(Properties.ToolDiameter);
            double insideRadius = 0.0125 * decimal.ToDouble(Properties.ToolDiameter);
            double innerArcOffset = 0.0625 * decimal.ToDouble(Properties.ToolDiameter);
            double maximumRadius = Math.Max(decimal.ToDouble(Properties.ToolDiameter), decimal.ToDouble(Properties.ShankDiameter)) / 2 + 0.1;

            ModelControllerTools.SetSketchDimension("EM_FLUTE_PROFILE_SKETCH",
                new string[] {"Angle", "LoftAngle", "LeadingEdgeDepth","TrailingEdgeDepth","InsideRadius","InnerArcOffset","MaximumRadius"},
                new double[] { angle, loftAngle, leadingEdgeDepth, trailingEdgeDepth, insideRadius, innerArcOffset, maximumRadius });
            ModelControllerTools.SetSketchDimension("EM_FLUTE_WASHOUT_PROFILE_SKETCH",
                new string[] { "Angle", "LoftAngle", "LeadingEdgeDepth", "TrailingEdgeDepth", "InsideRadius", "InnerArcOffset", "MaximumRadius" },
                new double[] { angle, loftAngle, leadingEdgeDepth, trailingEdgeDepth, insideRadius, innerArcOffset, maximumRadius });


            double loftSketchAngle = ((decimal.ToDouble(Properties.LOC) / GeneratedProperties.HelixPitch) * 360 - (2*angle/3)) % 360;
            ModelControllerTools.SetSketchDimension("EM_LOFT_PLANE_ANGLE_SKETCH",
                "LoftPlaneAngle",
                loftSketchAngle
                );

            // Set and enable pattern
            (CircularPatternFeatureData patternData, Action<object> patternApply) = ((CircularPatternFeatureData, Action<object>))ModelControllerTools.GetFeature("EM_FLUTE_PATTERN", "BODYFEATURE");
            patternData.TotalInstances = Properties.FluteCount;
            patternApply(patternData);
        }
    }
}
