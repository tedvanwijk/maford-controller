using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
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
            if (Properties.CoolantHole)
            {
                // TODO: enable CoolantPatternAlongFluting here as it is always going to be the case for end mills
                CoolantController coolantController = new CoolantController(Properties, GeneratedProperties, SwModel, EquationManager);
                coolantController.CreateCoolantHoles();
            }
            UpdateBlankConfiguration();
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

            if (Properties.Chipbreaker) SetChipbreakerDimensions();
        }

        private void UpdateBlankConfiguration()
        {
            // select blank config, then suppress entire EM folder
            SwModel.ShowConfiguration2("Blank");
            SwModel.ClearSelection2(true);
            SwModel.Extension.SelectByID2("EM", "FTRFOLDER", 0, 0, 0, false, 0, null, 0);
            SwModel.EditSuppress2();
        }

        private void SetChipbreakerDimensions()
        {
            double chipbreakerSpacing;
            float chipbreakerHelixAngle;
            if (Properties.ChipbreakerAlongCuttingHelix)
            {
                chipbreakerHelixAngle = 80.0f;
                (HelixFeatureData, Action<object>) featureData = ((HelixFeatureData, Action<object>))ModelControllerTools.GetFeature("EM_CHIPBREAKER_HELIX", "REFERENCECURVES");
                var (data, apply) = featureData;
                data.Clockwise = false;
                apply(data);
                chipbreakerSpacing = ((Math.PI * decimal.ToDouble(Properties.ToolDiameter)) / Properties.FluteCount) * Math.Sin((0.5f + (Properties.HelixAngle / 180.0f)) * Math.PI);
                chipbreakerSpacing /= Math.Sin(((chipbreakerHelixAngle - Properties.HelixAngle) * Math.PI) / 180);


            } else
            {
                chipbreakerHelixAngle = 70.0f;
                chipbreakerSpacing = ((Math.PI * decimal.ToDouble(Properties.ToolDiameter)) / Properties.FluteCount) * Math.Sin((0.5f - (Properties.HelixAngle / 180.0f)) * Math.PI);
                chipbreakerSpacing /= Math.Sin(((Properties.HelixAngle + chipbreakerHelixAngle) * Math.PI) / 180);
            }

            GeneratedProperties.ChipbreakerHelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(chipbreakerHelixAngle * Math.PI / 180);
            EquationController.SetEquation("EMChipbreakerHelixPitch", $"{GeneratedProperties.ChipbreakerHelixPitch}in");

            EquationController.SetEquation("EMChipbreakerPatternSpacing", $"{chipbreakerSpacing}in");
            ModelControllerTools.UnsuppressFeature("EM_CHIPBREAKER_PATTERN");
        }
    }
}
