using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SW_Console_Controller_V1.Lib
{
    internal static class ModelControllerTools
    {
        public static ModelDoc2 Model;
        public static ModelDocExtension ModelExtension;
        public static SelectionMgr SelectionManager;

        // TODO: index all features first, then deselect them all. That way, we don't need to change the selection all the time
        // Doesn't seem like there is a way to do this, but still something to look into

        static public void SelectFeature(string name, string type, bool append = false, int mark = 0)
        {
            Model.Extension.SelectByID2(name, type, 0, 0, 0, append, mark, null, 0);
        }
        
        static public void SetSketchDimension(string sketchName, string dimensionName, decimal dimension)
        {
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            Dimension dim = feature.Parameter(dimensionName);
            dim.SetValue3(decimal.ToDouble(dimension), 0, null);
            Model.ClearSelection2(true);
        }

        static public void SetSketchDimension(string sketchName, Dictionary<string, decimal> dimensions)
        {
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            foreach (KeyValuePair<string, decimal> entry in dimensions)
            {
                Dimension dim = feature.Parameter(entry.Key);
                dim.SetValue3(decimal.ToDouble(entry.Value), 0, null);
            }
            Model.ClearSelection2(true);
        }

        static public void SetSketchDimension(string sketchName, string[] dimensionNames, decimal[] dimensions)
        {
            if (dimensionNames.Length != dimensions.Length) return;
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            for (int i = 0; i < dimensionNames.Length; i++)
            {
                Dimension dim = feature.Parameter(dimensionNames[i]);
                dim.SetValue3(decimal.ToDouble(dimensions[i]), 0, null);
            }
            Model.ClearSelection2(true);
        }

        static public void SetSketchDimension(string sketchName, string dimensionName, double dimension)
        {
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            Dimension dim = feature.Parameter(dimensionName);
            dim.SetValue3(dimension, 0, null);
            Model.ClearSelection2(true);
        }

        static public void SetSketchDimension(string sketchName, Dictionary<string, double> dimensions)
        {
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            foreach (KeyValuePair<string, double> entry in dimensions)
            {
                Dimension dim = feature.Parameter(entry.Key);
                dim.SetValue3(entry.Value, 0, null);
            }
            Model.ClearSelection2(true);
        }

        static public void SetSketchDimension(string sketchName, string[] dimensionNames, double[] dimensions)
        {
            if (dimensionNames.Length != dimensions.Length) return;
            Model.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            for (int i = 0; i < dimensionNames.Length; i++)
            {
                Dimension dim = feature.Parameter(dimensionNames[i]);
                dim.SetValue3(dimensions[i], 0, null);
            }
            Model.ClearSelection2(true);
        }

        static public void UnsuppressFeature(string featureName)
        {
            Model.Extension.SelectByID2(featureName, "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
            Model.EditUnsuppress2();
            Model.ClearSelection2(true);
        }

        static public void Unsuppress(string name, string type)
        {
            Model.Extension.SelectByID2(name, type, 0, 0, 0, false, 0, null, 0);
            Model.EditUnsuppress2();
            Model.ClearSelection2(true);
        }

        public static void Apply(this Feature feature, object featureData, bool activateFeature)
        {
            feature.ModifyDefinition(featureData, Model, null);
            if (activateFeature) Model.EditUnsuppress2();
            Model.ClearSelection2(true);
        }

        static public (dynamic, Action<object>) GetFeature(string featureName, string featureType, bool activateFeature = true)
        {
            // Important: the changes have to be applied before anything else is done, as the feature only gets deselected once the changes have been applied.
            Model.Extension.SelectByID2(featureName, featureType, 0, 0, 0, false, 0, null, 0);
            Feature feature = SelectionManager.GetSelectedObject6(1, -1);
            dynamic featureData = feature.GetDefinition();
            return (featureData, newFeatureData => feature.Apply(newFeatureData, activateFeature));
        }

        static private void ApplyFeatureChanges(Feature feature, dynamic featureData)
        {
            feature.ModifyDefinition(featureData, Model, null);
        }

        public static decimal ConvertToMeters(this decimal input)
        {
            return input / 39.3701m;
        }

        public static double ConvertToMeters(this double input)
        {
            return input / 39.3701;
        }
    }
}
