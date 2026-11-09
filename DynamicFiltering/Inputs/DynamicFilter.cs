using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Inputs
{
    [Serializable]
    public class DynamicFilter
    {
        public Dictionary<string, List<string>> DynamicFilterFieldsMultiSelect { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> DynamicFilterFields { get; set; } = new Dictionary<string, string>();
    }
}
