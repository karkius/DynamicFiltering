using DynamicFiltering.Config;
using System;

namespace DynamicFiltering.Config
{
    public interface IDynamicFilterFieldConfigOverride
    {
        string ColName { get; set; }
        ValueTestOperatorsEnum? StringValueTestOperator { get; }
        DynamicFilterFieldTypes? FieldType { get;set; }
    }
}
