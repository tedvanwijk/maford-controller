using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class BlankController : ModelController
    {
        public BlankController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            CoolantController coolantController = new CoolantController(Properties, GeneratedProperties, SwModel, EquationManager);
            coolantController.CreateCoolantHoles();
        }
    }
}
