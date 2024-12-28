using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW_Console_Controller_V1.Models
{
    internal class Prp
    {
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
        public string DrawingType { get; set; }
    }
}
