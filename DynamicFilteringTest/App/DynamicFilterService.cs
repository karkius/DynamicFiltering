using DynamicFiltering;
using DynamicFiltering.Config;
using DynamicFiltering.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace DynamicFilteringTest.App
{
    /// <summary>
    /// Application-side glue: maps the filter enum to view names and supplies field overrides from the database.
    /// </summary>
    public static class DynamicFilterService
    {
        public static string ViewName(DynamicFiltersEnum filter)
        {
            return filter.GetAttributeOfType<DynamicFilterViewConfigAttribute>().ViewName;
        }

        /// <summary>
        /// Creates an empty filter for a list. Fill DynamicFilterFields / DynamicFilterFieldsMultiSelect from user input.
        /// </summary>
        public static DynamicFilterLogic Create(DynamicFiltersEnum filter)
        {
            return new DynamicFilterLogic(ViewName(filter));
        }

        /// <summary>
        /// Passed to <see cref="DynamicFilterUtil.AppStart"/>: the library calls it with a view name to get field overrides.
        /// </summary>
        public static IDynamicFilterConfigOverride GetConfigOverride(string viewName)
        {
            var filter = Enum.GetValues(typeof(DynamicFiltersEnum)).Cast<DynamicFiltersEnum>()
                .Where(f => ViewName(f) == viewName)
                .Cast<DynamicFiltersEnum?>()
                .FirstOrDefault();
            if (filter == null)
                return null;

            return new DynamicFilterConfigOverride
            {
                ViewName = viewName,
                FieldConfigs = CachedDynamicFilterConfigOverrides.Values(filter.Value).Cast<IDynamicFilterFieldConfigOverride>().ToList()
            };
        }

        /// <summary>
        /// Fields to render in the filter form. Returns null when the view does not exist in the database.
        /// </summary>
        public static DynamicFilterConfigVM GetFormConfig(DynamicFiltersEnum filter, DbContext ctx)
        {
            var filterConfig = Create(filter).GetDynamicFilterConfig(ctx);
            if (filterConfig == null) return null;
            var configFromDB = CachedDynamicFilterConfigOverrides.Values(filter);
            return new DynamicFilterConfigVM
            {
                Fields = filterConfig.FieldConfigs.Select(c =>
                {
                    var colCfgFromDB = configFromDB.SingleOrDefault(cdb => cdb.ColName == c.ColName);
                    return new DynamicFilterFieldVM
                    {
                        ColName = c.ColName,
                        DataType = c.FieldDataType,
                        DisplayName = colCfgFromDB?.DisplayName ?? c.ColName,
                        Order = colCfgFromDB?.Order ?? int.MaxValue,
                        FieldType = c.FieldType
                    };
                })
                .OrderBy(f => f.Order)
                .ToList()
            };
        }
    }

    public class DynamicFilterConfigOverride : IDynamicFilterConfigOverride
    {
        public string ViewName { get; set; }
        public List<IDynamicFilterFieldConfigOverride> FieldConfigs { get; set; }
    }
}
