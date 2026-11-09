using DynamicFiltering.Config;
using DynamicFiltering.Inputs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using LinqKit;

namespace DynamicFiltering
{
    /// <summary>
    /// Holds the values a user entered for one dynamic filter (one database view) and
    /// turns them into a query over the view that can be joined with any entity query.
    /// </summary>
    [Serializable]
    public class DynamicFilterLogic : DynamicFilter
    {
        private string ViewName { get; set; }

        /// <summary>
        /// Parameterless constructor for deserialization (e.g. MVC model binding / JSON).
        /// Use <see cref="MapValuesFrom"/> to copy the posted values into an instance created with a view name.
        /// </summary>
        public DynamicFilterLogic() { }

        public DynamicFilterLogic(string viewname)
        {
            ViewName = viewname;
        }

        /// <summary>
        /// Builds a query over the dynamic view type with all entered conditions applied.
        /// Returns null when there are no conditions or the view is not registered.
        /// </summary>
        /// <param name="applyIdSelect">true - project to the view's Id column; false - return whole view rows (used for composite ids).</param>
        public IQueryable FilteringQuery(DbContext ctx, bool applyIdSelect = true)
        {
            DynamicFilterConfig fieldConfigs = DynamicFilterConfig(ctx.GetType());
            if (fieldConfigs == null)
                return null;

            List<string> conditionString = new List<string>();
            List<object> sqlparams = new List<object>();
            int i = 0;
            foreach (var field in DynamicFilterFields ?? new Dictionary<string, string>())
            {
                if (string.IsNullOrEmpty(field.Value))
                    continue;

                DynamicFilterFieldConfig fieldConfig = FindFieldConfig(fieldConfigs, field.Key);
                if (fieldConfig == null) continue;

                string parameterName = i++.ToString();
                string @operator = "";
                if (fieldConfig.FieldType == DynamicFilterFieldTypes.Range)
                {
                    if (field.Key.EndsWith(DynamicFilterFieldConfig.SuffixFrom))
                    {
                        @operator = ">=";
                    }
                    else if (field.Key.EndsWith(DynamicFilterFieldConfig.SuffixTo))
                    {
                        @operator = "<=";
                    }
                }
                else if (fieldConfig.FieldType == DynamicFilterFieldTypes.Single)
                {
                    @operator = "=";
                }

                string colname = fieldConfig.ColName;
                switch (fieldConfig.FieldDataType.Name)
                {
                    case nameof(DateTime):
                        if (DateTime.TryParse(field.Value, out DateTime dateTimeValue))
                        {
                            conditionString.Add($"{colname} {@operator} @{parameterName}");
                            sqlparams.Add(dateTimeValue);
                        }
                        break;
                    case nameof(String):
                        if (fieldConfig.StringValueTestOperator == ValueTestOperatorsEnum.Contains)
                        {
                            conditionString.Add($"{colname}.Contains( @{parameterName} )");
                        }
                        else
                        {
                            conditionString.Add($"{colname} = @{parameterName}");
                        }
                        sqlparams.Add(field.Value);
                        break;
                    case nameof(Int32):
                        if (Int32.TryParse(field.Value, out var parsedInt32))
                        {
                            conditionString.Add($"{colname} {@operator} @{parameterName}");
                            sqlparams.Add(parsedInt32);
                        }
                        break;
                    case nameof(Int64):
                        if (Int64.TryParse(field.Value, out var parsedInt64))
                        {
                            conditionString.Add($"{colname} {@operator} @{parameterName}");
                            sqlparams.Add(parsedInt64);
                        }
                        break;
                    case nameof(Decimal):
                        if (TryParseDecimal(field.Value, out var parsedDecimal))
                        {
                            conditionString.Add($"{colname} {@operator} @{parameterName}");
                            sqlparams.Add(parsedDecimal);
                        }
                        break;
                    case nameof(Boolean):
                        var parseSucces = bool.TryParse(field.Value, out var parsedBool);
                        if (!parseSucces)
                        {
                            if (field.Value == "1") { parsedBool = true; parseSucces = true; }
                            if (field.Value == "0") { parsedBool = false; parseSucces = true; }
                        }
                        if (parseSucces)
                        {
                            conditionString.Add($"{colname} {@operator} @{parameterName}");
                            sqlparams.Add(parsedBool);
                        }
                        break;
                    default:
                        conditionString.Add($"{colname} {@operator} @{parameterName}");
                        sqlparams.Add(field.Value);
                        break;
                }
            }

            foreach (var field in DynamicFilterFieldsMultiSelect ?? new Dictionary<string, List<string>>())
            {
                if (field.Value == null || !field.Value.Any())
                    continue;
                var fieldConfig = FindFieldConfig(fieldConfigs, field.Key);
                if (fieldConfig == null) continue;
                string parameterName = i++.ToString();
                conditionString.Add($"@{parameterName}.Contains(outerIt.{fieldConfig.ColName})");//https://stackoverflow.com/a/39754926
                sqlparams.Add(field.Value.ToArray());
            }

            if (!conditionString.Any())
                return null;

            if (DynamicFilterUtil.DynamicTypes == null
                || !DynamicFilterUtil.DynamicTypes.TryGetValue(ctx.GetType(), out var typesOfCtx)
                || !typesOfCtx.TryGetValue(ViewName, out var filteringType))
                return null;

            string queryString = string.Join(" AND ", conditionString.Select(str => $"({str})"));
            IQueryable filterEntityQuery = ctx.Set(filteringType).AsQueryable().Where(queryString, sqlparams.ToArray());

            return applyIdSelect ? filterEntityQuery.Select("Id") : filterEntityQuery;
        }

        private static DynamicFilterFieldConfig FindFieldConfig(DynamicFilterConfig fieldConfigs, string fieldKey)
        {
            string colName = fieldKey.Replace(DynamicFilterFieldConfig.SuffixFrom, "").Replace(DynamicFilterFieldConfig.SuffixTo, "");
            return fieldConfigs.FieldConfigs.FirstOrDefault(f => f.ColName == colName);
        }

        /// <summary>
        /// Accepts both comma and period as the decimal separator; no thousands separators.
        /// </summary>
        private static bool TryParseDecimal(string value, out decimal result)
        {
            const NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                        | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            return Decimal.TryParse(value.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out result);
        }

        private IQueryable<TIdType> FilteringQuery<TIdType>(DbContext ctx)
        {
            return FilteringQuery(ctx) as IQueryable<TIdType>;
        }

        /// <summary>
        /// Joins <paramref name="baseQuery"/> with the filtering query on Id.
        /// Returns null when there is nothing to filter by.
        /// </summary>
        public IQueryable<TEntityType> FilteredQuery<TEntityType, TIdType>(
            DbContext ctx,
            IQueryable<TEntityType> baseQuery)
            where TEntityType : class, IHasId<TIdType>
        {
            var filteringQuery = FilteringQuery<TIdType>(ctx);
            if (filteringQuery == null)
                return null;

            return from item in baseQuery
                   join filteringItem in filteringQuery on item.Id equals filteringItem
                   select item;
        }

        public bool HasValues()
        {
            return DynamicFilterFields != null && DynamicFilterFields.Any(d => !string.IsNullOrEmpty(d.Value))
                ||
                (DynamicFilterFieldsMultiSelect != null && DynamicFilterFieldsMultiSelect.Any(d => d.Value != null && d.Value.Any()));
        }

        /// <summary>
        /// Filter expression for entities implementing IHasId&lt;int&gt;. Returns null when there is nothing to filter by.
        /// </summary>
        public Expression<Func<TEntity, bool>> FilterExprIntId<TEntity>(DbContext ctx)
            where TEntity : class, IHasId<int>
        {
            IQueryable<int> filteringQuery = FilteringQuery<int>(ctx);
            if (filteringQuery == null)
                return null;
            Expression<Func<TEntity, bool>> rezExpr = et => filteringQuery.Any(fid => fid == et.Id);
            return rezExpr;
        }

        /// <summary>
        /// Filter expression for any entity, with the int id selected by <paramref name="idSelExpr"/>.
        /// The query it is applied to must be <c>AsExpandable()</c> (LinqKit).
        /// Returns null when there is nothing to filter by.
        /// </summary>
        public Expression<Func<TEntity, bool>> FilterExprIntId<TEntity>(DbContext ctx, Expression<Func<TEntity, int>> idSelExpr)
            where TEntity : class
        {
            IQueryable<int> filteringQuery = FilteringQuery<int>(ctx);
            if (filteringQuery == null)
                return null;
            Expression<Func<TEntity, bool>> rezExpr = et => filteringQuery.Any(fid => fid == idSelExpr.Invoke(et));
            return rezExpr;
        }

        /// <summary>
        /// Filter expression for entities implementing IHasId&lt;string&gt;. Returns null when there is nothing to filter by.
        /// </summary>
        public Expression<Func<TEntity, bool>> FilterExprStringId<TEntity>(DbContext ctx)
            where TEntity : class, IHasId<string>
        {
            IQueryable<string> filteringQuery = FilteringQuery<string>(ctx);
            if (filteringQuery == null) return null;
            Expression<Func<TEntity, bool>> rezExpr = et => filteringQuery.Any(fid => fid == et.Id);
            return rezExpr;
        }

        /// <summary>
        /// Joins <paramref name="baseQuery"/> with the filtering query on (Id1, Id2).
        /// Returns null when there is nothing to filter by.
        /// </summary>
        public IQueryable<TEntityType> FilteredQuery<TEntityType, TIdType, TId2>(
            DbContext ctx,
            IQueryable<TEntityType> baseQuery)
            where TEntityType : class, IHasId<TIdType, TId2>
        {
            var filteringQuery = FilteringQueryCompositeId<TIdType, TId2>(ctx);
            if (filteringQuery == null)
                return null;

            return from item in baseQuery
                   join filteringItem in filteringQuery on new { item.Id1, item.Id2 } equals new { filteringItem.Id1, filteringItem.Id2 }
                   select item;
        }

        /// <summary>
        /// Filtering query for views with a composite (Id1, Id2) key.
        /// </summary>
        public IQueryable<IHasId<TId1, TId2>> FilteringQueryCompositeId<TId1, TId2>(DbContext ctx)
        {
            return FilteringQuery(ctx, false) as IQueryable<IHasId<TId1, TId2>>;
        }

        /// <summary>
        /// Filters all <typeparamref name="TEntity"/> rows by a composite (Id1, Id2) key selected by <paramref name="idSelExpr"/>.
        /// Returns null when there is nothing to filter by.
        /// </summary>
        public IQueryable<TEntity> FilteredQueryCompositeId<TEntity, TId1, TId2>(DbContext ctx, Expression<Func<TEntity, IHasId<TId1, TId2>>> idSelExpr)
        where TEntity : class
        {
            IQueryable<IHasId<TId1, TId2>> filteringQuery = FilteringQueryCompositeId<TId1, TId2>(ctx);
            if (filteringQuery == null)
                return null;

            var baseQuery = ctx.Set<TEntity>().AsExpandable();
            return from item in baseQuery
                   join filteringItem in filteringQuery on
                   new { idSelExpr.Invoke(item).Id1, idSelExpr.Invoke(item).Id2 } equals new { filteringItem.Id1, filteringItem.Id2 }
                   select item;
        }

        /// <summary>
        /// Adds (or replaces) the dynamic filter expression in a filter-expression dictionary, if any values were entered.
        /// </summary>
        public void AddExprsIfNeededIntId<TFilter, TEntity>(Dictionary<Expression<Func<TFilter, object>>,
                                                            Expression<Func<TEntity, bool>>> filterExprs, DbContext ctx)
            where TEntity : class, IHasId<int>
        {
            if (HasValues())
                SetFilterExpr(filterExprs, FilterExprIntId<TEntity>(ctx));
        }

        public void AddExprsIfNeededIntId<TFilter, TEntity>(Dictionary<Expression<Func<TFilter, object>>,
                                                    Expression<Func<TEntity, bool>>> filterExprs, DbContext ctx, Expression<Func<TEntity, int>> idSelExpr)
            where TEntity : class
        {
            if (HasValues())
                SetFilterExpr(filterExprs, FilterExprIntId(ctx, idSelExpr));
        }

        public void AddExprsIfNeededStringId<TFilter, TEntity>(Dictionary<Expression<Func<TFilter, object>>,
                                                            Expression<Func<TEntity, bool>>> filterExprs, DbContext ctx)
            where TEntity : class, IHasId<string>
        {
            if (HasValues())
                SetFilterExpr(filterExprs, FilterExprStringId<TEntity>(ctx));
        }

        private static void SetFilterExpr<TFilter, TEntity>(Dictionary<Expression<Func<TFilter, object>>, Expression<Func<TEntity, bool>>> filterExprs,
            Expression<Func<TEntity, bool>> expr)
        {
            if (expr == null)
                return;
            Expression<Func<TFilter, object>> dynamicFilterExprKey = f => "DynamicFilterExpr";
            // Expression keys compare by reference, so look up an existing dynamic-filter key by its text.
            var existingKey = filterExprs.Keys.FirstOrDefault(k => k.ToString() == dynamicFilterExprKey.ToString());
            filterExprs[existingKey ?? dynamicFilterExprKey] = expr;
        }

        internal DynamicFilterConfig DynamicFilterConfig(Type ctxType)
        {
            try
            {
                // the view does not exist in the database or was not registered for this context
                if (DynamicFilterUtil.DynamicTypes == null
                    || !DynamicFilterUtil.DynamicTypes.TryGetValue(ctxType, out var typesOfCtx)
                    || ViewName == null
                    || !typesOfCtx.TryGetValue(ViewName, out var viewType))
                {
                    return null;
                }

                IDynamicFilterConfigOverride configOverride = DynamicFilterUtil.GetDynamicFilterConfigOverride?.Invoke(ViewName);
                DynamicFilterConfig result = new DynamicFilterConfig { ViewName = ViewName };

                foreach (var prop in viewType.GetProperties())
                {
                    try
                    {
                        DynamicFilterFieldConfig field = new DynamicFilterFieldConfig(prop.Name, ExtractDataType(prop));

                        IDynamicFilterFieldConfigOverride fieldConfigOverride = configOverride?.FieldConfigs?.SingleOrDefault(fc => fc.ColName == prop.Name);
                        if (fieldConfigOverride != null)
                        {
                            if (fieldConfigOverride.FieldType != null) field.FieldType = fieldConfigOverride.FieldType.Value;
                            if (fieldConfigOverride.StringValueTestOperator != null) field.StringValueTestOperator = fieldConfigOverride.StringValueTestOperator.Value;
                        }

                        result.FieldConfigs.Add(field);
                    }
                    catch (Exception ex)
                    {
                        // one broken property should not break the whole filter
                        DynamicFilterUtil.Log(ex.ToString());
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                DynamicFilterUtil.Log(ex.ToString());
                return null;
            }
        }

        internal List<DynamicFilterFieldConfig> GetDynamicFilterFields(Type ctxType)
        {
            return DynamicFilterConfig(ctxType)?.FieldConfigs;
        }

        private static Type ExtractDataType(PropertyInfo prop)
        {
            return Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        }

        /// <summary>
        /// Fields (view columns) available for this filter, with overrides applied. Use it to render the filter UI.
        /// Returns null when the view is not registered.
        /// </summary>
        public DynamicFilterConfig GetDynamicFilterConfig(Type ctxType)
        {
            return DynamicFilterConfig(ctxType);
        }

        /// <summary>
        /// Same as <see cref="GetDynamicFilterConfig(Type)"/>, but first makes sure EF has built the context's model
        /// (that is when the view types are created), so it also works before the context has run any query.
        /// </summary>
        public DynamicFilterConfig GetDynamicFilterConfig(DbContext ctx)
        {
            _ = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)ctx).ObjectContext;
            return DynamicFilterConfig(ctx.GetType());
        }

        /// <summary>
        /// Copies entered values from another instance (e.g. one created by model binding without a view name).
        /// </summary>
        public void MapValuesFrom(DynamicFilterLogic baseDynamicFilter)
        {
            this.DynamicFilterFields = baseDynamicFilter.DynamicFilterFields;
            this.DynamicFilterFieldsMultiSelect = baseDynamicFilter.DynamicFilterFieldsMultiSelect;
        }
    }
}
