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

        // Center holes
        public bool UpperHole { get; set; }
        public bool LowerHole { get; set; }
        public bool UpperBoss { get; set; }
        public decimal A1Upper { get; set; }
        public decimal A1Lower { get; set; }
        public decimal A2Upper { get; set; }
        public decimal A2Lower { get; set; }
        public decimal D1Upper { get; set; }
        public decimal D2Upper { get; set; }
        public decimal D1Lower { get; set; }
        public decimal D2Lower { get; set; }
        public decimal LLower { get; set; }
        public decimal LUpper { get; set; }
        public decimal BossDiameter { get; set; }
        public decimal BossLength { get; set; }

        // Coolant
        public bool CoolantHole { get; set; }
        public decimal CoolantHoleAngle { get; set; }
        public decimal CoolantHoleLength { get; set; }
        public decimal CoolantHoleDiameter { get; set; }
        public int CoolantHoleCount { get; set; }
        public decimal CoolantHoleRotation { get; set; }
        public decimal CoolantFeedDiameter { get; set; }
        public int CoolantPatternCount { get; set; }
        public decimal CoolantPatternAngle { get; set; }
        public bool CoolantPatternAlongFluting { get; set; }
        public decimal CoolantPatternLength { get; set; }
    }
}
