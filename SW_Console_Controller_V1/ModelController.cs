using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1
{
    internal class ModelController
    {
        public Properties Properties { get; }
        public GeneratedProperties GeneratedProperties { get; set; }
        public ModelDoc2 SwModel { get; }
        public EquationMgr EquationManager { get; }
        public ModelController(Properties properties, GeneratedProperties generatedPropertes, ModelDoc2 model, EquationMgr equationManager)
        {
            Properties = properties;
            GeneratedProperties = generatedPropertes;
            SwModel = model;
            EquationManager = equationManager;
        }
    }
}
