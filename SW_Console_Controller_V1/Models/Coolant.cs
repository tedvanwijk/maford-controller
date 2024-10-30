using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Models
{
    internal class Coolant
    {
        public bool CoolantHole { get; set; }
        public decimal CoolantHoleAngle { get; set; }
        public decimal CoolantHoleLength { get; set; }
        public decimal CoolantHoleDiameter { get; set; }
        public int CoolantHoleCount { get; set; }
        public bool CoolantHoleEqualSpacing { get; set; }
        public decimal CoolantHoleRotation { get; set; }
        public decimal CoolantFeedDiameter { get; set; }
        public int CoolantPatternCount { get; set; }
        public decimal CoolantPatternAngle { get; set; }
        public bool CoolantPatternAlongFluting { get; set; }
        public decimal CoolantPatternLength { get; set; }
    }
}
