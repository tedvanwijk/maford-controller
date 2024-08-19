﻿using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Lib;
using SW_Console_Controller_V1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Controllers
{
    internal class StepController : ModelController
    {
        public StepController(Properties properties, GeneratedProperties generatedProperties, ModelDoc2 model, EquationMgr equationManager) : base(properties, generatedProperties, model, equationManager)
        {
        }
        public void CreateSteps()
        {
            string[] dimensionNames = new string[] { "Length", "Angle", "InnerDiameter", "OuterDiameter" };
            EquationController.SetEquation("DrillCoolantOffset", $"0.3125 * {Properties.Steps[0].Diameter}");
            EquationController.SetEquation("DrillCoolantDiameter", $"0.08 * {Properties.Steps[0].Diameter}");

            // rebuild before copying step sketch. When copying the sketch, it loses its external relations, but it is positioned the same as the copied sketch.
            // So, we have to make sure that the model does not change after creating the sketch cuts, which is why we rebuild in advance.
            // If later on there is some stuff after this happens that modifies/rebuilds the model maybe look into a different approach. Prefer not to 
            // set the relations in the new sketch again, because I suspect that that will be significantly slower. Maybe something with sketch blocks?
            // E.g. make 1 sketch block containing all steps with external relations in a higher sketch and make 1 step cut feature.
            SwModel.ForceRebuild3(false);

            for (int i = 0; i < Properties.Steps.Length; i++)
            {
                Step currentStep = Properties.Steps[i];
                decimal outerDiameter;
                if (i == Properties.Steps.Length - 1) outerDiameter = Properties.ToolDiameter;
                else outerDiameter = Properties.Steps[i + 1].Diameter;
                decimal[] dimensions = new decimal[]
                {
                    currentStep.Length,
                    currentStep.Angle,
                    currentStep.Diameter,
                    outerDiameter
                };

                ModelControllerTools.SetSketchDimension("STEP_SKETCH", dimensionNames, dimensions);

                ModelControllerTools.SelectFeature("STEP_SKETCH", "SKETCH");
                SwModel.EditCopy();
                ModelControllerTools.SelectFeature("Front Plane", "PLANE");
                SwModel.Paste();
                Feature sketchFeature = SwModel.Extension.GetLastFeatureAdded();
                string sketchName = $"STEP_{i}_SKETCH";
                sketchFeature.Name = sketchName;
                sketchFeature.Select2(false, 0);
                ModelControllerTools.SelectFeature("Line1@LENGTH_REF", "SKETCHSEGMENT", true, 4);
                Feature cut = SwModel.FeatureManager.FeatureRevolveCut2(
                    2 * Math.PI,
                    false,
                    0,
                    0,
                    0,
                    false,
                    true,
                    false,
                    false,
                    true
                    );
                cut.Name = $"STEP_{i}_CUT";
            }
        }
    }
}
