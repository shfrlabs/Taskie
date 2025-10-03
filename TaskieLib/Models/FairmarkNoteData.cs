using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskieLib.Models {
    public class FairmarkNoteData {
        public string id { get; set; }
        public string name { get; set; }
        public string emoji { get; set; }
        public Windows.UI.Color[] colors { get; set; }
    }
}
