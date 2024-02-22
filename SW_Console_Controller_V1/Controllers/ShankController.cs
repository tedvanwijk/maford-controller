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
        public ShankController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model) : base(properties, generatedProperties, model)
        {
            UpdateModel(); 
        }

        private void UpdateModel()
        {
            GeneratedProperties.ShankChamferWidth = Properties.ToolDiameter < 0.2501m ? 0.03m : 0.045m;
            GeneratedProperties.ShankChamferAngle = Properties.ToolDiameter < 0.2501m ? 30m : 45m;
            GeneratedProperties.ShankToHeadRadius = Properties.ShankEndAtHead ? (Properties.ToolDiameter - (Properties.ShankType == "Neck" ? Properties.ShankNeckDiameter : Properties.ShankDiameter)) / 2 : Properties.ShankToHeadRadius;
            switch (Properties.ShankType)
            {
                case "Reduced":
                    ModelControllerTools.SetSketchDimension("REDUCED_SHANK_SKETCH",
                        new[] { "ChamferAngle", "ChamferWidth", "ShankDiameter", "ToHeadRadius" },
                        new[] { GeneratedProperties.ShankChamferAngle, GeneratedProperties.ShankChamferWidth, Properties.ShankDiameter, GeneratedProperties.ShankToHeadRadius }
                        );
                    ModelControllerTools.UnsuppressFeature("REDUCED_SHANK_CUT");
                    break;
                case "Neck":
                    ModelControllerTools.SetSketchDimension("NECK_SHANK_SKETCH",
                        new[] { "ChamferAngle", "ChamferWidth", "NeckLength", "NeckDiameter", "BlendAngle", "ToHeadRadius" },
                        new[] { GeneratedProperties.ShankChamferAngle, GeneratedProperties.ShankChamferWidth, Properties.ShankNeckLength, Properties.ShankNeckDiameter, Properties.ShankBlendAngle, GeneratedProperties.ShankToHeadRadius }
                        );
                    ModelControllerTools.UnsuppressFeature("NECK_SHANK_CUT");
                    break;
                case "Blend":
                    ModelControllerTools.SetSketchDimension("BLEND_SHANK_SKETCH",
                        new[] { "ChamferAngle", "ChamferWidth", "BlendAngle" },
                        new[] { GeneratedProperties.ShankChamferAngle, GeneratedProperties.ShankChamferWidth, Properties.ShankBlendAngle}
                        );
                    ModelControllerTools.UnsuppressFeature("BLEND_SHANK_CUT");
                    break;
                case "Normal":
                    ModelControllerTools.SetSketchDimension("SHANK_SKETCH",
                        new[] { "ChamferAngle", "ChamferWidth" },
                        new[] { GeneratedProperties.ShankChamferAngle, GeneratedProperties.ShankChamferWidth }
                        );
                    ModelControllerTools.UnsuppressFeature("SHANK_CUT");
                    break;
            }
        }
    }
}
