using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Models
{
    internal class CenterDimensions
    {
        public decimal A1Min { get; set; }
        public decimal A1Max { get; set; }
        public decimal A2Min { get; set; }
        public decimal A2Max { get; set; }
        public decimal D1Min { get; set; }
        public decimal D1Max { get; set; }
        public decimal D2Min { get; set; }
        public decimal D2Max { get; set; }
        public decimal LMin { get; set; }
        public decimal LMax { get; set; }
        public decimal BossDiameterMin { get; set; }
        public decimal BossDiameterMax { get; set; }
        public decimal BossLengthMax { get; set; }
        public decimal BossLengthMin { get; set; }
    }
}
