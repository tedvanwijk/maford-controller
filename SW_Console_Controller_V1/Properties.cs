using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW_Console_Controller_V1.Models;

namespace SW_Console_Controller_V1
{
    internal class Properties
    {
        public string ToolType { get; set; }
        public string PartFileName { get; set; }
        public string DrawingFileName { get; set; }
        public int SpecificationNumber { get; set; }
        public string DimensionFileName { get; set; }
        public string ToleranceFileName { get; set; }
        public string ToolSeriesFileName { get; set; }
        public string ToolSeries { get; set; }
        public string ToolSeriesPath { get; set; }
        public string ToolSeriesOutputRange { get; set; }
        public string ToolSeriesInputRange { get; set; }
        public string DrawingType { get; set; }
        public string MasterPath { get; set; }
        public string ExecutablePath { get; set; }
        public string OutputPath { get; set; }
        public string[] ToolSeriesInputs { get; set; }

        // PRP sheet
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public string DrawnBy { get; set; }
        public string MAFordNumber { get; set; }
        public string Quote { get; set; }
        public string Customer { get; set; }
        public string EndUser { get; set; }
        public string Coater { get; set; }
        public string Coating { get; set; }
        public string LabelLine1 { get; set; }
        public string LabelLine2 { get; set; }
        public string MarkingLine1 { get; set; }
        public string MarkingLine2 { get; set; }
        public string PreTreatment { get; set; }
        public string PreTreatmentMachine { get; set; }
        public string PostTreatment { get; set; }
        public string PostTreatmentMachine { get; set; }
        public string Finish { get; set; }
        public string CompanyName { get; set; }
        public string Revision { get; set; }
        public bool FormingViewOnDrawing { get; set; }

        // Reference Dimensions
        public decimal LOA { get; set; }
        public decimal LOF { get; set; }
        public decimal LOC { get; set; }
        public decimal BodyLength { get; set; }
        public bool BodyLengthSameAsLOF { get; set; }
        public decimal ToolDiameter { get; set; }

        // General Shank
        public decimal ShankDiameter { get; set; }
        public string ShankType { get; set; }
        public decimal ShankToHeadRadius { get; set; }
        public bool ShankEndAtHead { get; set; }
        public decimal ShankBlendAngle { get; set; }
        public decimal ShankNeckLength { get; set; }
        public decimal ShankNeckDiameter { get; set; }

        // General Tool
        public int HelixAngle { get; set; }
        public int FluteCount { get; set; }

        // EM
        public string CornerStyle { get; set; }
        public decimal CornerRadius { get; set; }
        public decimal CornerChamferAngle { get; set; }
        public decimal CornerChamferWidth { get; set; }
        public bool Chipbreaker { get; set; }
        public bool ChipbreakerAlongCuttingHelix { get; set; }

        // Drill
        public decimal PointAngle { get; set; }
        public bool LOFFromPoint { get; set; }
        public bool CoolantThrough { get; set; }
        public bool StraightFlute { get; set; }

        // Steps
        public bool StepTool { get; set; }
        public Step[] Steps { get; set; }
    }
}
