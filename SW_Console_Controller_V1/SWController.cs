﻿using System;
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
using System.Globalization;

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
        private EquationMgr _equationManager;
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

        public SWController(Properties properties, SldWorks swApp, string input)
        {
            _swApp = swApp;

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            // set properties and determine path to master files
            _properties = properties;
            string masterPath = _properties.MasterPath;
            string outputPath = _properties.OutputPath;
            Directory.CreateDirectory(Path.Combine(outputPath, _properties.SpecificationNumber.ToString()));

            // Write data to json file in dir, for debug and/or issue solving purposes
            System.IO.File.WriteAllText(Path.Combine(outputPath, _properties.SpecificationNumber.ToString(), "data.json"), input);

            string oldDocumentPath = Path.Combine(masterPath, "TOOL_V2.SLDPRT");
            string newDocumentPath = Path.Combine(outputPath, $"{_properties.SpecificationNumber}/{_properties.PartFileName}.SLDPRT");
            string oldDrawingPath = Path.Combine(masterPath, "DRAWING_V2.SLDDRW");
            string newDrawingPath = Path.Combine(outputPath, $"{_properties.SpecificationNumber}/{_properties.DrawingFileName}.SLDDRW");

            // closes open files. Just in case a previous run left files open and unsaved
            _swApp.CloseAllDocuments(true);

#if DEBUG
            // removes the old files if present. Only in debug config for easy testing
            File.Delete(newDocumentPath);
            File.Delete(newDrawingPath);
#endif

            // copy the drawing along with the model
            _swApp.CopyDocument(
                oldDrawingPath,
                newDrawingPath,
                new string[] { oldDocumentPath },
                new string[] { newDocumentPath },
                1);
            _swModel = _swApp.OpenDoc6(newDocumentPath, 1, 1, "", ref _fileError, ref _fileWarning);
            _swModelExtension = _swModel.Extension;
            _equationManager = _swModel.GetEquationMgr();
            //EquationController.Manager = _equationManager;

            // Set prpsheet data
            _propertyManager = _swModelExtension.CustomPropertyManager[""];
            EquationController.Initialize(_propertyManager);

            SetPrpData();

            _selectionMgr = _swModel.SelectionManager;
            ModelControllerTools.Model = _swModel;
            ModelControllerTools.ModelExtension = _swModelExtension;
            ModelControllerTools.SelectionManager = _selectionMgr;

            SetReferences();
            if (properties.ToolType != "Blank") _shankController = new ShankController(properties, _generatedProperties, _swModel, _equationManager);
            _bodyController = new BodyController(properties, _generatedProperties, _swModel, _equationManager);
            _swModel.ForceRebuild3(false);

            _swDrawingModel = _swApp.OpenDoc6(newDrawingPath, 3, 1, "", ref _drawingError, ref _drawingWarning);
            _swDrawing = (DrawingDoc)_swDrawingModel;
            DrawingControllerTools.Model = _swDrawingModel;
            DrawingControllerTools.ModelExtension = _swDrawingModel.Extension;
            DrawingControllerTools.SelectionManager = _swDrawingModel.SelectionManager;
            DrawingControllerTools.Drawing = _swDrawing;
            DrawingControllerTools.Properties = _properties;
            DrawingControllerTools.GeneratedProperties = _generatedProperties;
            DrawingDimensionTools.Model = _swModel;
            DrawingDimensionTools.Drawing = _swDrawing;
            DrawingDimensionTools.DrawingModel = _swDrawingModel;
            DrawingDimensionTools.Properties = _properties;
            _drawingController = new DrawingController(properties, _generatedProperties, _swDrawingModel, _swDrawing, _equationManager);

            int activationError = 0;
            _swApp.ActivateDoc3($"{_properties.PartFileName}.SLDPRT", false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);

            if (_properties.LeftHandSpiral)
            {
                if (_properties.LeftHandSpiral) MirrorModel();
                _swModel.ForceRebuild3(false);
            }

            foreach (string partFileType in _properties.PartFileTypes) _swModel.Extension.SaveAs3(Path.Combine(_properties.OutputPath, $"{_properties.SpecificationNumber}/{_properties.PartFileName}.{partFileType}"), 0, 1, null, null, ref _saveError, ref _saveWarning);

            CreateThumbnail();

            _swModel.Save3(1, ref _saveError, ref _saveWarning);

            _swApp.ActivateDoc3($"{_properties.DrawingFileName}.SLDDRW", false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);

            _swDrawingModel.Save3(1, ref _drawingSaveError, ref _drawingSaveWarning);

            foreach (string drawingFileType in _properties.DrawingFileTypes) _swDrawingModel.Extension.SaveAs3(Path.Combine(_properties.OutputPath, $"{_properties.SpecificationNumber}/{_properties.DrawingFileName}.{drawingFileType}"), 0, 1, null, null, ref _saveError, ref _saveWarning);
#if !DEBUG
            //closes both files if not in debug config
            _swApp.CloseAllDocuments(false);
#endif
        }

        private void CreateThumbnail()
        {
            // resize window to match tool ratio
            double LOA = decimal.ToDouble(_properties.LOA);
            double maxD = decimal.ToDouble(_generatedProperties.MaxDiameter);

            int width = 1500;
            int height = (int)Math.Ceiling(width * maxD / LOA);

            ModelView view = _swModel.ActiveView;

            // For width, add twice the frame width. For height, add toolbar height (32), bottom frame height (8) and motion study bar (16)
            view.FrameState = 0;
            view.FrameWidth = width + 8 + 8;
            view.FrameHeight = height + 32 + 8 + 16;

            // After resizing, zoom to model (uses meters)
            maxD = maxD.ConvertToMeters();
            LOA = LOA.ConvertToMeters();
            _swModel.ShowNamedView2("thumbnail", -1);
            _swModel.SetFeatureManagerWidth(0);
            // y, x, z! SW docs is lying to me
            _swModel.ViewZoomTo2(0, maxD / 2, 0, LOA, -maxD / 2, 0);

            // Hide everything (sketches, curves, etc.)
            _swModel.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewDisplayHideAllTypes, true);

            string imageFileName = Path.Combine(_properties.ImagePath, $"{_properties.SpecificationNumber}.png");
            _swModelExtension.SaveAs3(imageFileName, 0, 1, null, null, ref _saveError, ref _saveWarning);

            _swModel.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewDisplayHideAllTypes, false);

#if DEBUG
            view.FrameState = 1;
            int activationError = 0;
            _swApp.ActivateDoc3($"{_properties.DrawingFileName}.SLDDRW", false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);
#endif
        }

        private void SetPrpData()
        {
            _propertyManager.Add3("PartNo", 30, _properties.Prp.PartNumber, 2);
            _propertyManager.Add3("DESCRIPTION", 30, _properties.Prp.Description, 2);
            _propertyManager.Add3("DrawnBy", 30, _properties.Prp.DrawnBy, 2);
            _propertyManager.Add3("M.A.Ford NUMBER", 30, _properties.Prp.MAFordNumber, 2);
            _propertyManager.Add3("QUOTE", 30, _properties.Prp.Quote, 2);
            _propertyManager.Add3("CUSTOMER", 30, _properties.Prp.Customer, 2);
            _propertyManager.Add3("END USER", 30, _properties.Prp.EndUser, 2);
            _propertyManager.Add3("COATER", 30, _properties.Prp.Coater, 2);
            _propertyManager.Add3("COATING", 30, _properties.Prp.Coating, 2);
            _propertyManager.Add3("LABEL LINE 1", 30, _properties.Prp.LabelLine1, 2);
            _propertyManager.Add3("LABEL LINE 2", 30, _properties.Prp.LabelLine2, 2);
            _propertyManager.Add3("MARKING LINE 1", 30, _properties.Prp.MarkingLine1, 2);
            _propertyManager.Add3("MARKING LINE 2", 30, _properties.Prp.MarkingLine2, 2);
            _propertyManager.Add3("PRE TREATMENT", 30, _properties.Prp.PreTreatment, 2);
            _propertyManager.Add3("PRE TREATMENT MACHINE", 30, _properties.Prp.PreTreatmentMachine, 2);
            _propertyManager.Add3("POST TREATMENT", 30, _properties.Prp.PostTreatment, 2);
            _propertyManager.Add3("POST TREATMENT MACHINE", 30, _properties.Prp.PostTreatmentMachine, 2);
            _propertyManager.Add3("Finish", 30, _properties.Prp.Finish, 2);
            _propertyManager.Add3("COMPANYNAME", 30, _properties.Prp.CompanyName, 2);
            _propertyManager.Add3("Revision", 30, _properties.Prp.Revision, 2);
        }

        private void SetReferences()
        {
            EquationController.SetEquation("LOA", _properties.LOA);
            EquationController.SetEquation("LOC", _properties.LOC);
            EquationController.SetEquation("LOF", _properties.LOF);
            // note: BodyLengthSameAsLOF will be used for all tool types, regardless of if that tool type uses LOC or LOF as its unit for flute length
            _generatedProperties.BodyLength = _properties.BodyLengthSameAsLOF ? _properties.LOF : _properties.BodyLength;
            EquationController.SetEquation("BodyLength", _generatedProperties.BodyLength);

            _generatedProperties.MaxDiameter = Math.Max(_properties.ShankDiameter, _properties.ToolDiameter);
            decimal maxDiameterOffset = _generatedProperties.MaxDiameter + 0.5m;

            EquationController.SetEquation("ToolDiameter", _properties.ToolDiameter);
            EquationController.SetEquation("ShankDiameter", _properties.ShankDiameter);
            EquationController.SetEquation("MaxDiameter", _generatedProperties.MaxDiameter);
            EquationController.SetEquation("MaxDiameterOffset", maxDiameterOffset);

            if (_properties.StepTool) _generatedProperties.TopStepDiameter = _properties.Steps[0].Diameter;
            else _generatedProperties.TopStepDiameter = _properties.ToolDiameter;
            EquationController.SetEquation("TopStepDiameter", _generatedProperties.TopStepDiameter);

            if (_generatedProperties.MaxDiameter > _properties.ToolDiameter) ModelControllerTools.UnsuppressFeature("BODY_PROFILE_CUT");
        }

        private void MirrorModel()
        {
            ModelControllerTools.UnsuppressFeature("MIRROR");

            // Get the raw feature object for the DELETE feature
            (Feature, Action<object>) featureData = ((Feature, Action<object>))ModelControllerTools.GetFeature("DELETE", "BODYFEATURE", true, true);
            var (data, apply) = featureData;
            // Get the feature definition object
            DeleteBodyFeatureData deleteData = (DeleteBodyFeatureData)(data.GetDefinition());

            // Get the bodies in the part file
            PartDoc part = (PartDoc)_swModel;
            var bodies = part.GetBodies2(-1, false);

            // Access selections for the DELETE feature (necessary for changing feature data object)
            deleteData.AccessSelections(_swModel, null);

            // Loop through bodies in file. Skip MIRROR body and set Bodies array (there should only be 2 bodies in file)
            foreach (Body2 body in bodies)
            {
                if (body.Name == "MIRROR") continue;
                deleteData.Bodies = new Body2[] { body };
            }

            // Modify the feature definition with the adjusted data object
            data.ModifyDefinition(deleteData, _swModel, null);

            // Unsuppress DELETE feature
            ModelControllerTools.UnsuppressFeature("DELETE");
        }
    }
}
