using DynamicFiltering.Config;
using DynamicFiltering.Util;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;

namespace DynamicFiltering
{
    public class DynamicFilterUtil
    {
        /// <summary>
        /// Key = DbContext type, value = (view name -> type built at runtime from the view's columns).
        /// </summary>
        public static Dictionary<Type, Dictionary<string, Type>> DynamicTypes;
        internal static Dictionary<string, DynamicFilterViewConfigAttribute> DynamicViewConfigAttributes = new Dictionary<string, DynamicFilterViewConfigAttribute>();
        internal static Func<string, IDynamicFilterConfigOverride> GetDynamicFilterConfigOverride;
        internal static Action<string> LoggingFunc { get; set; }

        public static bool AppStarted { get { return appStarted; } }
        private static bool appStarted = false;

        /// <summary>
        /// Call once at application start, before the first DbContext is created.
        /// Until this is called, <see cref="RegisterDynamicTypes"/> does nothing
        /// (so design-time tools such as EF migrations don't need the views to exist).
        /// </summary>
        /// <param name="getDynamicFilterConfigOverride">Returns per-field overrides for a view name (may return null). Optional.</param>
        /// <param name="loggingFunc">Receives diagnostic messages (missing views, bad columns, timeouts). Optional.</param>
        public static void AppStart(Func<string, IDynamicFilterConfigOverride> getDynamicFilterConfigOverride = null, Action<string> loggingFunc = null)
        {
            GetDynamicFilterConfigOverride = getDynamicFilterConfigOverride;
            LoggingFunc = loggingFunc;
            appStarted = true;
        }

        /// <summary>
        /// Call from <c>DbContext.OnModelCreating</c>. For each value of <paramref name="dynamicFilterViewsEnumType"/>
        /// reads the columns of the view named in its <see cref="DynamicFilterViewConfigAttribute"/>, builds a type for it
        /// and registers that type as an entity. Views that can't be read are skipped.
        /// <see cref="AppStart"/> must have been called first.
        /// </summary>
        /// <param name="connectionString">Connection string to read view schemas with.</param>
        /// <param name="connectionStringName">Name of the connection string in app/web.config; used when <paramref name="connectionString"/> is null. Defaults to the context type name.</param>
        public static void RegisterDynamicTypes(DbModelBuilder modelBuilder, Type contextType, Type dynamicFilterViewsEnumType, string connectionString = null,
            string connectionStringName = null)
        {
            if (!dynamicFilterViewsEnumType.IsEnum)
            {
                throw new DynamicFilterException(nameof(dynamicFilterViewsEnumType) + " must be enum type");
            }

            if (!appStarted) return;

            foreach (Enum view in EnumExtensions.GetEnumValues(dynamicFilterViewsEnumType))
            {
                DynamicFilterViewConfigAttribute viewConfig = view.GetAttributeOfType<DynamicFilterViewConfigAttribute>();
                if (viewConfig == null)
                {
                    throw new DynamicFilterException($"{dynamicFilterViewsEnumType.Name}.{view} has no {nameof(DynamicFilterViewConfigAttribute)}");
                }
                if (!DynamicViewConfigAttributes.ContainsKey(viewConfig.ViewName))
                {
                    DynamicViewConfigAttributes.Add(viewConfig.ViewName, viewConfig);
                }
                var type = DynamicTypeBuilder.BuildTypeByTable(viewConfig.ViewName, viewConfig.BaseType, $"SELECT TOP 1 * FROM {viewConfig.ViewName}",
                    new SqlParameter[] { }
                    , connectionStringName ?? contextType.Name
                    , connectionString);
                if (type == null)
                {
                    continue;
                }

                DynamicTypes = DynamicTypes ?? new Dictionary<Type, Dictionary<string, Type>>();

                if (!DynamicTypes.ContainsKey(contextType))
                {
                    DynamicTypes.Add(contextType, new Dictionary<string, Type>());
                }
                if (!DynamicTypes[contextType].ContainsKey(viewConfig.ViewName))
                {
                    DynamicTypes[contextType].Add(viewConfig.ViewName, type);
                    modelBuilder.RegisterEntityType(type);
                }
            }
        }

        internal static void Log(string msg)
        {
            LoggingFunc?.Invoke(msg);
        }
    }
}
