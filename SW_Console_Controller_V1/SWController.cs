using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.sldworks;
using SW_Console_Controller_V1.Controllers;
using SW_Console_Controller_V1.Lib;
using System.IO;
using System.Dynamic;

namespace SW_Console_Controller_V1
{
    internal class SWController
    {
        private SldWorks _swApp;
        private ModelDoc2 _swModel;
        private ModelDocExtension _swModelExtension;
        private SelectionMgr _selectionMgr;
        private ModelDoc2 _swDrawingModel;
        private DrawingDoc _swDrawing;
        private Properties _properties;
        private GeneratedProperties _generatedProperties = new GeneratedProperties();
        private CustomPropertyManager _propertyManager;
        private int _fileError;
        private int _fileWarning;
        private int _saveError;
        private int _saveWarning;
        private int _drawingError;
        private int _drawingWarning;
        private int _drawingSaveError;
        private int _drawingSaveWarning;

        // Controllers
        private ShankController _shankController;
        private BodyController _bodyController;
        private DrawingController _drawingController;

        public SWController(Properties properties)
        {
            // set properties and determine path to master files
            _properties = properties;
            string masterPath = _properties.MasterPath;
            Directory.CreateDirectory(Path.Combine(masterPath, _properties.SpecificationNumber.ToString()));
            string oldDocumentPath = Path.Combine(masterPath, "TOOL_V2.SLDPRT");
            string newDocumentPath = Path.Combine(masterPath, $"{_properties.SpecificationNumber}/{_properties.PartFileName}.SLDPRT");
            string oldDrawingPath = Path.Combine(masterPath, "DRAWING_V2.SLDDRW");
            string newDrawingPath = Path.Combine(masterPath, $"{_properties.SpecificationNumber}/{_properties.DrawingFileName}.SLDDRW");

            _swApp = new SldWorks();
            // TODO: unsafe! Deletes unsaved documents for work in dev, remove before use!!!
            _swApp.CloseAllDocuments(true);
            // TODO: dev lines, remove the generated files prior to generating new ones. Remove for production
            File.Delete(newDocumentPath);
            File.Delete(newDrawingPath);

            // copy the drawing along with the model
            _swApp.CopyDocument(
                oldDrawingPath,
                newDrawingPath,
                new string[] { oldDocumentPath },
                new string[] { newDocumentPath },
                1);
            _swModel = _swApp.OpenDoc6(newDocumentPath, 1, 1, "", ref _fileError, ref _fileWarning);
            _swModelExtension = _swModel.Extension;

            // Set prpsheet data
            _propertyManager = _swModelExtension.CustomPropertyManager[""];

            SetPrpData();

            _selectionMgr = _swModel.SelectionManager;
            ModelControllerTools.Model = _swModel;
            ModelControllerTools.ModelExtension = _swModelExtension;
            ModelControllerTools.SelectionManager = _selectionMgr;

            SetReferences();
            _shankController = new ShankController(properties, _generatedProperties, _swModel);
            _bodyController = new BodyController(properties, _generatedProperties, _swModel);

            _swModel.ForceRebuild3(false);
            _swModel.Save3(1, ref _saveError, ref _saveWarning);
            //_swApp.CloseDoc(newDocumentPath);

            _swDrawingModel = _swApp.OpenDoc6(newDrawingPath, 3, 1, "", ref _drawingError, ref _drawingWarning);
            _swDrawing = (DrawingDoc)_swDrawingModel;
            DrawingControllerTools.Model = _swDrawingModel;
            DrawingControllerTools.ModelExtension = _swDrawingModel.Extension;
            DrawingControllerTools.SelectionManager = _swDrawingModel.SelectionManager;
            DrawingControllerTools.Drawing = _swDrawing;
            DrawingControllerTools.Properties = _properties;
            _drawingController = new DrawingController(properties, _generatedProperties, _swDrawingModel, _swDrawing);
            _swDrawingModel.Save3(1, ref _drawingSaveError, ref _drawingSaveWarning);
        }

        private void SetPrpData()
        {
            _propertyManager.Add3("PartNo", 30, _properties.PartNumber, 2);
            _propertyManager.Add3("DESCRIPTION", 30, _properties.Description, 2);
            _propertyManager.Add3("DrawnBy", 30, _properties.DrawnBy, 2);
            _propertyManager.Add3("M.A.Ford NUMBER", 30, _properties.MAFordNumber, 2);
            _propertyManager.Add3("QUOTE", 30, _properties.Quote, 2);
            _propertyManager.Add3("CUSTOMER", 30, _properties.Customer, 2);
            _propertyManager.Add3("END USER", 30, _properties.EndUser, 2);
            _propertyManager.Add3("COATER", 30, _properties.Coater, 2);
            _propertyManager.Add3("COATING", 30, _properties.Coating, 2);
            _propertyManager.Add3("LABEL LINE 1", 30, _properties.LabelLine1, 2);
            _propertyManager.Add3("LABEL LINE 2", 30, _properties.LabelLine2, 2);
            _propertyManager.Add3("MARKING LINE 1", 30, _properties.MarkingLine1, 2);
            _propertyManager.Add3("MARKING LINE 2", 30, _properties.MarkingLine2, 2);
            _propertyManager.Add3("PRE TREATMENT", 30, _properties.PreTreatment, 2);
            _propertyManager.Add3("PRE TREATMENT MACHINE", 30, _properties.PreTreatmentMachine, 2);
            _propertyManager.Add3("POST TREATMENT", 30, _properties.PostTreatment, 2);
            _propertyManager.Add3("POST TREATMENT MACHINE", 30, _properties.PostTreatmentMachine, 2);
            _propertyManager.Add3("Finish", 30, _properties.Finish, 2);
            _propertyManager.Add3("COMPANYNAME", 30, _properties.CompanyName, 2);
            _propertyManager.Add3("Revision", 30, _properties.Revision, 2);
        }

        private void SetReferences()
        {
            ModelControllerTools.SetSketchDimension("LENGTH_REF",
                new[] { "LOA", "LOC", "LOF", "BodyLength" },
                new[] { _properties.LOA, _properties.LOC, _properties.LOF, _properties.BodyLength }
                );

            decimal maxDiameter = Math.Max(_properties.ShankDiameter, _properties.ToolDiameter);
            decimal maxDiameterOffset = maxDiameter + 0.5m;
            Dictionary<string, decimal> refDimensions = new Dictionary<string, decimal>
            {
                { "MaxDiameter", maxDiameter },
                { "MaxDiameterOffset", maxDiameterOffset },
                { "MaxBodyDiameter", _properties.ToolDiameter }
            };
            if (_properties.ShankType == "Reduced")
            {
                refDimensions.Add("MaxShankDiameter", _properties.ShankDiameter + 2 * _properties.ShankToHeadRadius);
            } else
            {
                refDimensions.Add("MaxShankDiameter", _properties.ShankDiameter);
            }
            ModelControllerTools.SetSketchDimension("DIAMETER_REF", refDimensions);

            if (maxDiameter > _properties.ToolDiameter)
            {
                ModelControllerTools.UnsuppressFeature("BODY_PROFILE_CUT");
            }
        }
    }
}
