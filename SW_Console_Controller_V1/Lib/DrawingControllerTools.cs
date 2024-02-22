using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Globalization;
using SolidWorks.Interop.swconst;

namespace SW_Console_Controller_V1.Lib
{
    internal static class DrawingControllerTools
    {
        public static ModelDoc2 Model;
        public static ModelDocExtension ModelExtension;
        public static SelectionMgr SelectionManager;
        public static DrawingDoc Drawing;
        public static Properties Properties;

        static public void HideDimension(string sheetName, string viewName, string dimensionName, bool deleteDimension = false)
        {
            ModelExtension.SelectByID2($"{dimensionName}@{Properties.PartFileName}@{sheetName}:{viewName}", "DIMENSION", 0, 0, 0, false, 0, null, 0);
            if (deleteDimension)
            {
                ModelExtension.DeleteSelection2(1);
            }
            else
            {
                Model.HideDimension();
            }
            Model.ClearSelection2(true);
        }

        static public void HideDimensions(string sheetName, string viewName, string[] dimensionNames, bool deleteDimension = false)
        {
            for (int i = 0; i < dimensionNames.Length; i++)
            {
                ModelExtension.SelectByID2($"{dimensionNames[i]}@{Properties.PartFileName}@{sheetName}:{viewName}", "DIMENSION", 0, 0, 0, true, 0, null, 0);
            }
            if (deleteDimension)
            {
                ModelExtension.DeleteSelection2(1);
            }
            else
            {
                Model.HideDimension();
            }
            Model.ClearSelection2(true);
        }

        static public void HideDimensions(string sheetName, string[] viewNames, string[] dimensionNames, bool deleteDimension = false)
        {
            if (viewNames.Length != dimensionNames.Length) return;
            for (int i = 0; i < viewNames.Length; i++)
            {
                ModelExtension.SelectByID2($"{dimensionNames[i]}@{Properties.PartFileName}@{sheetName}:{viewNames[i]}", "DIMENSION", 0, 0, 0, true, 0, null, 0);
            }
            if (deleteDimension)
            {
                ModelExtension.DeleteSelection2(1);
            }
            else
            {
                Model.HideDimension();
            }
            Model.ClearSelection2(true);
        }

        static public void HideDimensions(string[] sheetNames, string[] viewNames, string[] dimensionNames, bool deleteDimension = false)
        {
            if (viewNames.Length != dimensionNames.Length || dimensionNames.Length != sheetNames.Length) return;
            for (int i = 0; i < viewNames.Length; i++)
            {
                ModelExtension.SelectByID2($"{dimensionNames[i]}@{Properties.PartFileName}@{sheetNames[i]}:{viewNames[i]}", "DIMENSION", 0, 0, 0, true, 0, null, 0);
            }
            if (deleteDimension)
            {
                ModelExtension.DeleteSelection2(1);
            }
            else
            {
                Model.HideDimension();
            }
            Model.ClearSelection2(true);
        }

        static public void MoveDimension(Annotation annotation, double[] viewOutline, string relSide, (double relX, double relY) offSetPosition)
        {
            double[] relPos = new double[2];
            double viewSideSpacing = (viewOutline[2] - viewOutline[0] - decimal.ToDouble(Properties.LOA).ConvertToMeters()) / 2;
            switch (relSide)
            {
                case "TL":
                    relPos[0] = viewOutline[0];
                    relPos[1] = viewOutline[3];
                    break;
                case "T":
                    relPos[0] = (viewOutline[0] + viewOutline[2]) / 2;
                    relPos[1] = viewOutline[3];
                    break;
                case "TR":
                    relPos[0] = viewOutline[2];
                    relPos[1] = viewOutline[3];
                    break;
                case "R":
                    relPos[0] = viewOutline[2];
                    relPos[1] = (viewOutline[1] + viewOutline[3]) / 2;
                    break;
                case "BR":
                    relPos[0] = viewOutline[2];
                    relPos[1] = viewOutline[1];
                    break;
                case "B":
                    relPos[0] = (viewOutline[0] + viewOutline[2]) / 2;
                    relPos[1] = viewOutline[1];
                    break;
                case "BL":
                    relPos[0] = viewOutline[0];
                    relPos[1] = viewOutline[1];
                    break;
                case "L":
                    relPos[0] = viewOutline[0];
                    relPos[1] = (viewOutline[1] + viewOutline[3]) / 2;
                    break;
                case "C":
                    relPos[0] = (viewOutline[0] + viewOutline[2]) / 2;
                    relPos[1] = (viewOutline[1] + viewOutline[3]) / 2;
                    break;
                case "TBODY":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.BodyLength).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[3];
                    break;
                case "BBODY":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.BodyLength).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[1];
                    break;
                case "TTOOL":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.LOC).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[3];
                    break;
                case "BTOOL":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.LOC).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[1];
                    break;
                case "TNECK":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.ShankNeckLength).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[3];
                    break;
                case "BNECK":
                    relPos[0] = decimal.ToDouble(Properties.LOA - Properties.ShankNeckLength).ConvertToMeters() + viewSideSpacing + viewOutline[0];
                    relPos[1] = viewOutline[1];
                    break;
            }

            annotation.SetPosition2((relPos[0] + offSetPosition.relX.ConvertToMeters()), (relPos[1] + offSetPosition.relY.ConvertToMeters()), 0);
        }

        static public void SetDimensionTolerance(DisplayDimension displayDimension, DataRow toleranceData)
        {
            Dimension dimension = displayDimension.GetDimension2(0);
            string toleranceType = (string)toleranceData["TOL_TYPE"];
            int toleranceTypeIndex = 0;
            // don't ask me why the indices are in this order, ask the sw devs
            switch (toleranceType)
            {
                case "BASIC":
                    toleranceTypeIndex = 1;
                    break;
                case "BILAT":
                    toleranceTypeIndex = 2;
                    break;
                case "BLOCK":
                    toleranceTypeIndex = 10;
                    break;
                case "FIT":
                    toleranceTypeIndex = 7;
                    break;
                case "FITTOLONLY":
                    toleranceTypeIndex = 9;
                    break;
                case "FITWITHTOL":
                    toleranceTypeIndex = 8;
                    break;
                case "GENERAL":
                    toleranceTypeIndex = 11;
                    break;
                case "LIMIT":
                    toleranceTypeIndex = 3;
                    break;
                case "MAX":
                    toleranceTypeIndex = 6;
                    break;
                case "METRIC":
                    toleranceTypeIndex = 7;
                    break;
                case "MIN":
                    toleranceTypeIndex = 5;
                    break;
                case "SYMMETRIC":
                    toleranceTypeIndex = 4;
                    break;
            }
            // get the current tolerance and set the type
            DimensionTolerance tolerance = dimension.Tolerance;
            tolerance.Type = toleranceTypeIndex;
            // figure out if the tolerance is absolute or relative (most will be absolute)
            string valueType = (string)toleranceData["VAL_TYPE"];
            decimal minVal = decimal.Parse((string)toleranceData["MIN_VAL"], CultureInfo.InvariantCulture);
            decimal maxVal = decimal.Parse((string)toleranceData["MAX_VAL"], CultureInfo.InvariantCulture);
            if (valueType == "ABS")
            {
                decimal dimensionValue = (decimal)dimension.GetValue3(1, "")[0];
                minVal -= dimensionValue;
                maxVal -= dimensionValue;
            }

            // convert the value to meters or radians, depending on the type of dimension
            int dimensionType = dimension.GetType();
            if (dimensionType == (int)swDimensionParamType_e.swDimensionParamTypeDoubleAngular)
            {
                minVal = (decimal)(decimal.ToDouble(minVal) * Math.PI / 180);
                maxVal = (decimal)(decimal.ToDouble(maxVal) * Math.PI / 180);
            } else if (dimensionType == (int)swDimensionParamType_e.swDimensionParamTypeDoubleLinear)
            {
                minVal = minVal.ConvertToMeters();
                maxVal = maxVal.ConvertToMeters();
            }

            // set tolerance precision
            int digits = (int)toleranceData["VAL_DIGITS"];
            displayDimension.SetPrecision3(-1, -1, digits, digits);

            // TODO: implement table tolerances
            // for some reason, setvalues2 does not work. setvalues is deprecated but it does work
            //bool ret = tolerance.SetValues2(0.0127, 0.0127, 1, "");
            tolerance.SetValues(decimal.ToDouble(minVal), decimal.ToDouble(maxVal));
        }

        public static decimal ConvertToInches(this decimal input)
        {
            return input * 39.3701m;
        }

        public static decimal[] ConvertToInches(this decimal[] input)
        {
            return Array.ConvertAll(input, e => e * 39.3701m);
        }

        public static double ConvertToInches(this double input)
        {
            return input * 39.3701;
        }

        public static double[] ConvertToInches(this double[] input)
        {
            return Array.ConvertAll(input, e => e * 39.3701);
        }
    }
}
