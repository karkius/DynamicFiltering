using DynamicFiltering.Config;
using System;
using System.Collections.Generic;

namespace DynamicFilteringTest.App
{
    /// <summary>
    /// What a UI needs to render the filter form: one entry per view column.
    /// </summary>
    public class DynamicFilterConfigVM
    {
        public List<DynamicFilterFieldVM> Fields { get; set; } = new List<DynamicFilterFieldVM>();
    }

    public class DynamicFilterFieldVM
    {
        public Type DataType { get; set; }
        public string DisplayName { get; set; }
        public string ColName { get; set; }
        public int Order { get; set; }
        public DynamicFilterFieldTypes FieldType { get; set; }

        /// <summary>Input name for a range's lower bound.</summary>
        public string FromKey => ColName + DynamicFilterFieldConfig.SuffixFrom;
        /// <summary>Input name for a range's upper bound.</summary>
        public string ToKey => ColName + DynamicFilterFieldConfig.SuffixTo;
    }
}
