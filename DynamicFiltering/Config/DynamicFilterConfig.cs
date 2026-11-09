using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Config
{
    public class DynamicFilterConfig
    {
        public DynamicFilterConfig(){
            FieldConfigs = new List<DynamicFilterFieldConfig>();
        }

        public string ViewName { get; set; }
        public List<DynamicFilterFieldConfig> FieldConfigs { get; set; }
    }
}
