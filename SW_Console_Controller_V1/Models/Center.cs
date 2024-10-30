using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Models
{
    internal class Center
    {
        public bool UpperCenter { get; set; }
        public bool LowerCenter { get; set; }
        public string UpperType { get; set; }
        public string LowerType { get; set; }
        public CenterDimensions UpperCenterDimensions { get; set; }
        public CenterDimensions LowerCenterDimensions { get; set; }
        public bool UpperBoss { get; set; }
    }
}
