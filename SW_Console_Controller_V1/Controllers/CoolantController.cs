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

            if (Properties.ToolType == "End Mill")
            {
                coolantExitFluteRotation = 45m;
                if (!Properties.StraightFlute) coolantExitFluteRotation += (Properties.LOC - Properties.LOA + Properties.Coolant.CoolantHoleLength) / (decimal)GeneratedProperties.HelixPitch * 360m;
            } else if (Properties.ToolType == "Reamer")
            {
                // TODO: change 45 offset value for reamer flute profile
                coolantExitFluteRotation = 100m;
                if (!Properties.StraightFlute) coolantExitFluteRotation += (Properties.LOC - Properties.LOA + Properties.Coolant.CoolantHoleLength) / (decimal)GeneratedProperties.HelixPitch * 360m;
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
            decimal spacing;
            if (Properties.Coolant.CoolantHoleEqualSpacing) spacing = 360m / Properties.Coolant.CoolantHoleCount;
            else spacing = Properties.Coolant.CoolantHoleRotation;
            EquationController.SetEquation("CoolantSpacing", spacing);

            // the rotation angle describes the rotation of the lateral holes over the pattern height
            // E.g. a 6-fluted tool with 3 laterals spaced 120 degrees apart can be rotated
            // 60 degrees at a lower height so it effectively cools all 6 flutes
            decimal rotationAngle = Properties.Coolant.CoolantPatternAngle;

            if (Properties.Coolant.CoolantPatternAlongFluting && !Properties.StraightFlute)
            {
                // if false, the supplied rotation angle is the full angle.
                // if true, the coolant holes are for a conventional flute, and the laterals need to be
                // rotated the specified amount of degrees + the rotation of the fluting along the height of the pattern

                rotationAngle += 360m * Properties.Coolant.CoolantPatternLength / (decimal)GeneratedProperties.HelixPitch;
            }

            if (Properties.Coolant.CoolantPatternCount != 1)
            {
                if (rotationAngle == 0m) rotationAngle = 360m;
                Properties.Coolant.CoolantPattern = true;
                double coolantPatternSpacing = Math.Sqrt(Math.Pow(decimal.ToDouble(Properties.Coolant.CoolantPatternLength), 2f) + Math.Pow(decimal.ToDouble(rotationAngle) * Math.PI * decimal.ToDouble(Properties.ToolDiameter) / 360f, 2f));
                double coolantPatternPitch = 360f * decimal.ToDouble(Properties.Coolant.CoolantPatternLength) / decimal.ToDouble(rotationAngle);

                EquationController.SetEquation("CoolantHelixSpacing", coolantPatternSpacing);
                EquationController.SetEquation("CoolantHelixPitch", coolantPatternPitch);
                EquationController.SetEquation("CoolantPatternLength", Properties.Coolant.CoolantPatternLength);
            }
            else Properties.Coolant.CoolantPattern = false;
            EquationController.SetEquation("CoolantHelixCount", Properties.Coolant.CoolantPatternCount);

            ModelControllerTools.UnsuppressFeature("COOLANT_HOLE_PATTERN");
            ModelControllerTools.UnsuppressFeature("COOLANT_FEED_HOLE_CUT");
            ModelControllerTools.Unsuppress("COOLANT_DRAWING_SKETCH", "SKETCH");
            ModelControllerTools.Unsuppress("COOLANT_HOLE_DRAWING_SKETCH", "SKETCH");
            if (Properties.ToolType != "Blank") ModelControllerTools.UnsuppressFeature("COOLANT_SLOT_CUT");
        }
    }
}
