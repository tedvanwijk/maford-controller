using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using SW_Console_Controller_V1.Models;

namespace SW_Console_Controller_V1.Controllers
{
    internal class CenterController : ModelController
    {
        public CenterController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
        }

        public void CreateCenterHoles()
        {
            if (Properties.Center.UpperCenter)
            {
                EquationController.SetEquation("CenterHoleTopA1", Properties.Center.UpperCenterDimensions.A1Min);
                EquationController.SetEquation("CenterHoleTopA2", Properties.Center.UpperCenterDimensions.A2Min);
                EquationController.SetEquation("CenterHoleTopD1", Properties.Center.UpperCenterDimensions.D1Min);
                EquationController.SetEquation("CenterHoleTopD2", Properties.Center.UpperCenterDimensions.D2Min);
                EquationController.SetEquation("CenterHoleTopL", Properties.Center.UpperCenterDimensions.LMin);

                if (Properties.Center.UpperBoss)
                {
                    EquationController.SetEquation("CenterHoleBossLength", Properties.Center.UpperCenterDimensions.BossLength);
                    EquationController.SetEquation("CenterHoleBossDiameter", Properties.Center.UpperCenterDimensions.BossDiameter);
                    ModelControllerTools.UnsuppressFeature("CENTER_HOLE_TOP_BOSS");
                } else
                {
                    ModelControllerTools.UnsuppressFeature("CENTER_HOLE_TOP_CUT");
                }
            }

            if (Properties.Center.LowerCenter)
            {
                EquationController.SetEquation("CenterHoleBottomA1", Properties.Center.LowerCenterDimensions.A1Min);
                EquationController.SetEquation("CenterHoleBottomA2", Properties.Center.LowerCenterDimensions.A2Min);
                EquationController.SetEquation("CenterHoleBottomD1", Properties.Center.LowerCenterDimensions.D1Min);
                EquationController.SetEquation("CenterHoleBottomD2", Properties.Center.LowerCenterDimensions.D2Min);
                EquationController.SetEquation("CenterHoleBottomL", Properties.Center.LowerCenterDimensions.LMin);
                ModelControllerTools.UnsuppressFeature("CENTER_HOLE_BOTTOM_CUT");
            }
        }
    }
}
