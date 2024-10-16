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
            EquationController.SetEquation("CenterHoleTopA1", Properties.A1Upper);

            if (Properties.UpperHole)
            {
                EquationController.SetEquation("CenterHoleTopA1", Properties.A1Upper);
                EquationController.SetEquation("CenterHoleTopA2", Properties.A2Upper);
                EquationController.SetEquation("CenterHoleTopD1", Properties.D1Upper);
                EquationController.SetEquation("CenterHoleTopD2", Properties.D2Upper);
                EquationController.SetEquation("CenterHoleTopL", Properties.LUpper);

                if (Properties.UpperBoss)
                {
                    EquationController.SetEquation("CenterHoleBossLength", Properties.BossLength);
                    EquationController.SetEquation("CenterHoleBossDiameter", Properties.BossDiameter);
                    ModelControllerTools.UnsuppressFeature("CENTER_HOLE_TOP_BOSS");
                } else
                {
                    ModelControllerTools.UnsuppressFeature("CENTER_HOLE_TOP_CUT");
                }
            }

            if (Properties.LowerHole)
            {
                EquationController.SetEquation("CenterHoleBottomA1", Properties.A1Lower);
                EquationController.SetEquation("CenterHoleBottomA2", Properties.A2Lower);
                EquationController.SetEquation("CenterHoleBottomD1", Properties.D1Lower);
                EquationController.SetEquation("CenterHoleBottomD2", Properties.D2Lower);
                EquationController.SetEquation("CenterHoleBottomL", Properties.LLower);
                ModelControllerTools.UnsuppressFeature("CENTER_HOLE_BOTTOM_CUT");
            }
        }
    }
}
