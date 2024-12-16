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
            GeneratedProperties.HelixPitch = Math.PI * decimal.ToDouble(Properties.ToolDiameter) / Math.Tan(Properties.HelixAngle * Math.PI / 180f);
            EquationController.SetEquation("FluteHelixPitch", GeneratedProperties.HelixPitch);
            EquationController.SetEquation("FluteCount", Properties.FluteCount);


            // TODO: check if this should happen for all tool types
            CenterController centerController = new CenterController(Properties, GeneratedProperties, SwModel, EquationManager);
            centerController.CreateCenterHoles();

            switch (Properties.ToolType)
            {
                case "End Mill":
                    _toolController = new EMController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Drill":
                    _toolController = new DrillController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Reamer":
                    _toolController = new ReamerController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
                case "Blank":
                    _toolController = new BlankController(Properties, GeneratedProperties, SwModel, EquationManager);
                    break;
            }

            SwModel.ShowConfiguration2("Default");

            if (Properties.LeftHandSpiral)
            {
                ModelControllerTools.UnsuppressFeature("MIRROR");

                // Get the raw feature object for the DELETE feature
                (Feature, Action<object>) featureData = ((Feature, Action<object>))ModelControllerTools.GetFeature("DELETE", "BODYFEATURE", true, true);
                var (data, apply) = featureData;
                // Get the feature definition object
                DeleteBodyFeatureData deleteData = (DeleteBodyFeatureData)(data.GetDefinition());

                // Get the bodies in the part file
                PartDoc part = (PartDoc)SwModel;
                var bodies = part.GetBodies2(-1, false);

                // Access selections for the DELETE feature (necessary for changing feature data object)
                deleteData.AccessSelections(SwModel, null);

                // Loop through bodies in file. Skip MIRROR body and set Bodies array (there should only be 2 bodies in file)
                foreach (Body2 body in bodies)
                {
                    if (body.Name == "MIRROR") continue;
                    deleteData.Bodies = new Body2[] { body };
                }

                // Modify the feature definition with the adjusted data object
                data.ModifyDefinition(deleteData, SwModel, null);

                // Unsuppress DELETE feature
                ModelControllerTools.UnsuppressFeature("DELETE");
            }
        }
    }
}
