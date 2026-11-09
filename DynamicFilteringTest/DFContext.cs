using DynamicFiltering;
using DynamicFilteringTest.App;
using DynamicFilteringTest.Entities;
using System.Data.Entity;

namespace DynamicFilteringTest
{
    public class DFContext : DbContext
    {
        static DFContext()
        {
            // The sample creates its schema with SQL (DatabaseSetup); don't let EF try to.
            Database.SetInitializer<DFContext>(null);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Reads each view's columns and registers a generated entity type for it.
            // Runs once per AppDomain (EF caches the model).
            DynamicFilterUtil.RegisterDynamicTypes(modelBuilder, typeof(DFContext), typeof(DynamicFiltersEnum), this.Database.Connection.ConnectionString);
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<DynamicFilterConfigOverrideEntity> DynamicFilterConfigs { get; set; }
    }
}
