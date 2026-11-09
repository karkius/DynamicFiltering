using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Config
{
    public enum DynamicFilterFieldTypes
    {
        Single,
        Range,
        /// <summary>
        /// values of a multiselect field are compared strictly with "="
        /// </summary>
        Multiselect
        
    }
}
