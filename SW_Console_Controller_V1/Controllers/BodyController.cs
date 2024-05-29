using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class BodyController : ModelController
    {
        ModelController _toolController;
        public BodyController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
            UpdateModel();
        }

        private void UpdateModel()
        {
            GeneratedProperties.HelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(Properties.HelixAngle * Math.PI / 180);
            EquationManager.Equation[14] = $"FluteHelixPitch= {GeneratedProperties.HelixPitch}in";
            EquationManager.Equation[15] = $"FluteCount= {Properties.FluteCount}in";

            switch (Properties.ToolType)
            {
                case "End Mill":
                    _toolController = new EMController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
            }
        }
    }
}
