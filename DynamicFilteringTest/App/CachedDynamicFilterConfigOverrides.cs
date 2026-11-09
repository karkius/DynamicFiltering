using DynamicFilteringTest.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DynamicFilteringTest.App
{
    /// <summary>
    /// Caches field overrides per filter so the database is not hit on every request.
    /// </summary>
    public static class CachedDynamicFilterConfigOverrides
    {
        private static ConcurrentDictionary<DynamicFiltersEnum, List<DynamicFilterConfigOverrideEntity>> _cache
            = new ConcurrentDictionary<DynamicFiltersEnum, List<DynamicFilterConfigOverrideEntity>>();

        public static List<DynamicFilterConfigOverrideEntity> Values(DynamicFiltersEnum type)
        {
            return _cache.GetOrAdd(type, t =>
            {
                using (DFContext ctx = new DFContext())
                {
                    return ctx.DynamicFilterConfigs.AsNoTracking().Where(c => c.Type == t).ToList();
                }
            });
        }

        public static void Invalidate()
        {
            _cache.Clear();
        }
    }
}
