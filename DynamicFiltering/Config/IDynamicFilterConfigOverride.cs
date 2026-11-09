using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Config
{
    public interface IDynamicFilterConfigOverride
    {
        string ViewName{get;set;}
        List<IDynamicFilterFieldConfigOverride> FieldConfigs{get;set; }
    }
}
