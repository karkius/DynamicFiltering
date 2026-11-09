using DynamicFiltering.Config;
using DynamicFilteringTest.Entities;

namespace DynamicFilteringTest.App
{
    /// <summary>
    /// One value per dynamically filtered list. The attribute names the database view whose columns
    /// become filter fields, and the base type the generated type derives from (it provides the Id key).
    /// </summary>
    public enum DynamicFiltersEnum
    {
        [DynamicFilterViewConfig("DynamicFilterTest_CustomerView", typeof(EntityBase))]
        Customers = 0,
        [DynamicFilterViewConfig("DynamicFilterTest_OrderView", typeof(EntityBase))]
        Orders = 1,
        [DynamicFilterViewConfig("DynamicFilterTest_MissingView", typeof(EntityBase))]
        NotDeployedYet = 2,
    }
}
