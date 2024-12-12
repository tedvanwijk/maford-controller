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
        public string DimensionPath { get; set; }
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
        public Prp Prp { get; set; }

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
        public bool StraightFlute { get; set; }
        public bool LeftHandSpiral { get; set; }

        // EM & Reamer
        public string CornerStyle { get; set; }
        public decimal CornerChamferAngle { get; set; }
        public decimal CornerChamferWidth { get; set; }

        // EM
        public decimal CornerRadius { get; set; }
        public bool Chipbreaker { get; set; }
        public bool ChipbreakerAlongCuttingHelix { get; set; }

        // Drill
        public decimal PointAngle { get; set; }
        public bool LOFFromPoint { get; set; }
        public bool CoolantThrough { get; set; }

        // Steps
        public bool StepTool { get; set; }
        public Step[] Steps { get; set; }

        // Center holes
        public Center Center { get; set; }

        // Coolant
        public Coolant Coolant { get; set; }
        public bool Blank { get; set; }
    }
}
