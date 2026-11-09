using DynamicFiltering.Config;
using DynamicFilteringTest.App;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicFilteringTest.Entities
{
    /// <summary>
    /// Per-field settings stored in the database, so an admin can change how a view column is shown and compared
    /// without redeploying. Fields without a row use defaults derived from the column type.
    /// </summary>
    public class DynamicFilterConfigOverrideEntity : IDynamicFilterFieldConfigOverride
    {
        [Key, Column(Order = 0)]
        public DynamicFiltersEnum Type { get; set; }
        [Key, Column(Order = 1)]
        public string ColName { get; set; }

        public string DisplayName { get; set; }
        public DynamicFilterFieldTypes? FieldType { get; set; }
        public ValueTestOperatorsEnum? StringValueTestOperator { get; set; }
        public int? Order { get; set; }
    }
}
