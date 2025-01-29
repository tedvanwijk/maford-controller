using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Models
{
    internal class Step
    {
        public decimal Length { get; set; }
        public decimal Diameter { get; set; }
        public decimal Angle { get; set; }
        public decimal RTop { get; set; }
        public decimal RBottom { get; set; }
        public bool Midpoint { get; set; }
        public bool LOFFromPoint { get; set; }
    }
}
