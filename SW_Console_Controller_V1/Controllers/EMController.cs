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
            if (Properties.Coolant.CoolantHole)
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
            double angle = 360f / decimal.ToDouble(Properties.FluteCount) * 0.844f;
            double loftSketchAngle = ((decimal.ToDouble(Properties.LOC) / GeneratedProperties.HelixPitch) * 360f - (2f * angle / 3f)) % 360f;
            EquationController.SetEquation("EMLoftPlaneAngle", loftSketchAngle);

            // Set and enable pattern
            ModelControllerTools.UnsuppressFeature("EM_FLUTE_PATTERN");

            // Set corner style options
            EquationController.SetEquation("EMCornerRadius", Properties.CornerRadius);
            EquationController.SetEquation("EMCornerChamferAngle", Properties.CornerChamferAngle);
            EquationController.SetEquation("EMCornerChamferWidth", Properties.CornerChamferWidth);

            // Set loc rotation and helix angle
            EquationController.SetEquation("EMFluteLOCRotation", (decimal.ToDouble(Properties.LOC) / GeneratedProperties.HelixPitch * 360f) % 360f);
            EquationController.SetEquation("EMHelixAngle", Properties.HelixAngle);
            EquationController.SetEquation("EMFluteWashoutHelixPitch", GeneratedProperties.HelixPitch * 2f);

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
            SetFlutingProfileDimensions();
            if (Properties.Chipbreaker) SetChipbreakerDimensions();
        }

        private void SetFlutingProfileDimensions()
        {
            // fluting
            double fluteProfileAngle = 360f / Properties.FluteCount * 0.844f;
            EquationController.SetEquation("EMFluteProfileAngle", fluteProfileAngle);
            EquationController.SetEquation("EMFluteProfileLoftAngle", (2f/3f) * fluteProfileAngle);
            EquationController.SetEquation("EMFluteProfileLeadingEdgeDepth", 0.1m * Properties.ToolDiameter);
            decimal trailingEdgeDepth = 0.1m * Properties.ToolDiameter;
            EquationController.SetEquation("EMFluteProfileTrailingEdgeDepth", trailingEdgeDepth);
            EquationController.SetEquation("EMFluteProfileInsideRadius", 0.0125m * Properties.ToolDiameter);
            EquationController.SetEquation("EMFluteProfileInnerArcOffset", 0.0625m * Properties.ToolDiameter);
            EquationController.SetEquation("EMFluteProfileMaximumRadius", GeneratedProperties.MaxDiameter / 2m + 0.1m);

            // loft cut
            decimal loftTopWidth = 0.75m * Properties.ToolDiameter * 4m * (1m / Properties.FluteCount);
            EquationController.SetEquation("EMLoftTopWidth", loftTopWidth);
            EquationController.SetEquation("EMLoftBottomWidth", 0.15m * Properties.ToolDiameter * 4m * (1m / Properties.FluteCount));
            double loftInternalAngle = 90f - (Properties.HelixAngle * Math.Sqrt(Math.Pow(decimal.ToDouble(Properties.ToolDiameter) / 2f, 2f) + (Math.Pow(decimal.ToDouble(loftTopWidth), 2f) / 4f))) / (decimal.ToDouble(Properties.ToolDiameter) / 2f) + (Properties.HelixAngle * decimal.ToDouble(trailingEdgeDepth) / (decimal.ToDouble(Properties.ToolDiameter / 2m)));
            EquationController.SetEquation("EMLoftInternalAngle", loftInternalAngle);
            EquationController.SetEquation("EMLoftInternalRadius", 0.0375m * Properties.ToolDiameter);
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
                chipbreakerSpacing /= Math.Sin(((chipbreakerHelixAngle - Properties.HelixAngle) * Math.PI) / 180f);


            } else
            {
                chipbreakerHelixAngle = 70.0f;
                chipbreakerSpacing = ((Math.PI * decimal.ToDouble(Properties.ToolDiameter)) / Properties.FluteCount) * Math.Sin((0.5f - (Properties.HelixAngle / 180.0f)) * Math.PI);
                chipbreakerSpacing /= Math.Sin(((Properties.HelixAngle + chipbreakerHelixAngle) * Math.PI) / 180f);
            }

            GeneratedProperties.ChipbreakerHelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(chipbreakerHelixAngle * Math.PI / 180f);
            EquationController.SetEquation("EMChipbreakerHelixPitch", GeneratedProperties.ChipbreakerHelixPitch);
            EquationController.SetEquation("EMChipbreakerCutRotation", 360f / Properties.FluteCount);
            EquationController.SetEquation("EMChipbreakerPatternSpacing", chipbreakerSpacing);
            ModelControllerTools.UnsuppressFeature("EM_CHIPBREAKER_PATTERN");
        }
    }
}
