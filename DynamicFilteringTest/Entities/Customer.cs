using DynamicFiltering;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicFilteringTest.Entities
{
    /// <summary>
    /// The entity the list shows. Note it has no City, order totals etc. -
    /// those come from the views and can be changed in SQL without recompiling.
    /// </summary>
    [Table("DynamicFilterTest_Customer")]
    public class Customer : EntityBase, IHasId<int>
    {
        public string Name { get; set; }
    }
}
