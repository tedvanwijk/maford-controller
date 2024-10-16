using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class CoolantController : ModelController
    {
        public CoolantController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
        }

        public void CreateCoolantHoles()
        {
            // rotation of the fluting at the chosen start height of the highest lateral
            decimal coolantExitFluteRotation = 360m;

            if (Properties.ToolType != "Blank")
            {
                // TODO: this + 45 is the location of the coolant lateral exit from the vertical (z) axis for an end mill flute. Might be different for other tool types (reamer)
                coolantExitFluteRotation = (Properties.LOC - Properties.LOA + Properties.Coolant.CoolantHoleLength) / (decimal)GeneratedProperties.HelixPitch * 360m + 45m;
            }
            EquationController.SetEquation("CoolantExitFluteRotation", coolantExitFluteRotation);

            EquationController.SetEquation("CoolantStartLength", Properties.Coolant.CoolantHoleLength);
            EquationController.SetEquation("CoolantExitAngle", Properties.Coolant.CoolantHoleAngle);
            EquationController.SetEquation("CoolantHoleDiameter", Properties.Coolant.CoolantHoleDiameter);
            EquationController.SetEquation("CoolantFeedHoleDiameter", Properties.Coolant.CoolantFeedDiameter);
            EquationController.SetEquation("CoolantCount", Properties.Coolant.CoolantHoleCount);
            EquationController.SetEquation("CoolantExitWidth", Properties.ToolDiameter + 0.01m);
            EquationController.SetEquation("CoolantHelixLength", Properties.LOF);
            EquationController.SetEquation("CoolantSlotWidth", 1.05m * Properties.Coolant.CoolantFeedDiameter);
            EquationController.SetEquation("CoolantExitFluteRotation", decimal.ToDouble(Properties.LOC - Properties.LOA + Properties.Coolant.CoolantHoleLength) / GeneratedProperties.HelixPitch * 360f + 45f);
            decimal spacing;
            if (Properties.Coolant.CoolantHoleEqualSpacing) spacing = 360m / Properties.Coolant.CoolantHoleCount;
            else spacing = Properties.Coolant.CoolantHoleRotation;
            EquationController.SetEquation("CoolantSpacing", spacing);

            // the rotation angle describes the rotation of the lateral holes over the pattern height
            // E.g. a 6-fluted tool with 3 laterals spaced 120 degrees apart can be rotated
            // 60 degrees at a lower height so it effectively cools all 6 flutes
            decimal rotationAngle = Properties.Coolant.CoolantPatternAngle;

            if (Properties.Coolant.CoolantPatternAlongFluting)
            {
                // if false, the supplied rotation angle is the full angle.
                // if true, the coolant holes are for a conventional flute, and the laterals need to be
                // rotated the specified amount of degrees + the rotation of the fluting along the height of the pattern

                rotationAngle += 360m * Properties.Coolant.CoolantPatternLength / (decimal)GeneratedProperties.HelixPitch;
            }

            if (Properties.Coolant.CoolantPatternCount != 1)
            {
                double coolantPatternSpacing = Math.Sqrt(Math.Pow(decimal.ToDouble(Properties.Coolant.CoolantPatternLength), 2f) + Math.Pow(decimal.ToDouble(rotationAngle) * Math.PI * decimal.ToDouble(Properties.ToolDiameter) / 360f, 2f));
                double coolantPatternPitch = 360f * decimal.ToDouble(Properties.Coolant.CoolantPatternLength) / decimal.ToDouble(rotationAngle);

                EquationController.SetEquation("CoolantHelixSpacing", coolantPatternSpacing);
                EquationController.SetEquation("CoolantHelixPitch", coolantPatternPitch);
            }
            EquationController.SetEquation("CoolantHelixCount", Properties.Coolant.CoolantPatternCount);

            ModelControllerTools.UnsuppressFeature("COOLANT_HOLE_PATTERN");
            ModelControllerTools.UnsuppressFeature("COOLANT_FEED_HOLE_CUT");
            if (Properties.ToolType != "Blank") ModelControllerTools.UnsuppressFeature("COOLANT_SLOT_CUT");
        }
    }
}
