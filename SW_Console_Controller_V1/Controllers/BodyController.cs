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
        public BodyController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model) : base(properties, generatedProperties, model)
        {
            UpdateModel();
        }

        private void UpdateModel()
        {
            (HelixFeatureData helixData, Action<object> helixApply) = ((HelixFeatureData, Action<object>))ModelControllerTools.GetFeature("FLUTE_HELIX", "REFCURVE");
            helixData.Height = decimal.ToDouble((Properties.LOC).ConvertToMeters());
            GeneratedProperties.HelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(Properties.HelixAngle * Math.PI / 180);
            helixData.Pitch = GeneratedProperties.HelixPitch.ConvertToMeters();
            helixApply(helixData);

            switch (Properties.ToolType)
            {
                case "End Mill":
                    _toolController = new EMController(Properties, GeneratedProperties, SwModel);
                    break;
            }
        }
    }
}
