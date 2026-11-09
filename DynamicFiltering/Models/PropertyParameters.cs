using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering
{
    internal class PropertyParameters
    {
        public string PropName { get; set; }
        //public string DisplayFormatString { get; set; }
        public Type Type { get; internal set; }

        /// <summary>
        /// DisplayAttribute
        /// </summary>
        //public string DisplayName { get; set; }
    }
}
