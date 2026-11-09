# DynamicFiltering

Filter Entity Framework 6 queries by the columns of a SQL Server **view**, defined at runtime.

You write a view that returns an `Id` and any columns you want users to filter by. At application start the library reads the view's schema, **emits a .NET type for it** and registers it in your `DbContext`. User input (`"City" in [Vilnius, Kaunas]`, `"TotalAmount" >= 100`, …) is turned into a query over that view and **joined with your entity query by `Id`**. The result is one SQL statement.

To add a new filter field, change the view. You don't need to recompile or migrate.

## How it works

1. `DynamicFilterUtil.AppStart(...)` is called once at startup.
2. In `OnModelCreating`, `DynamicFilterUtil.RegisterDynamicTypes(...)` runs `SELECT TOP 1 * FROM <view>` for each view (schema only), builds a type with one property per column (`System.Reflection.Emit`), and registers it with `modelBuilder.RegisterEntityType`.
3. `DynamicFilterLogic` holds the entered values and builds a `Where` over the view type with System.Linq.Dynamic:
   - `Range` fields (dates, numbers): `>=` / `<=`, keys suffixed with `DynamicFilterFieldConfig.SuffixFrom` / `SuffixTo`
   - `Single` fields: `=`, or `Contains` for strings
   - `Multiselect` fields: `IN (...)`
4. The view query is joined with your entity query: `FilterExprIntId` / `FilterExprStringId` return a `Where` expression, and `FilteredQuery` returns a joined `IQueryable`. Composite `(Id1, Id2)` keys are supported.

If a view is missing or can't be read, it is skipped and logged, and that filter returns `null`. The rest of the application keeps working.

## Usage

```csharp
// 1. Declare filters: enum value -> view name + base type that provides the Id key
public enum DynamicFiltersEnum
{
    [DynamicFilterViewConfig("CustomerFilterView", typeof(EntityBase))]
    Customers,
}

// 2. Startup (both arguments optional)
DynamicFilterUtil.AppStart(
    getDynamicFilterConfigOverride: viewName => LoadOverridesFromDb(viewName), // per-field type / operator overrides
    loggingFunc: msg => log.Warn(msg));

// 3. DbContext
protected override void OnModelCreating(DbModelBuilder modelBuilder)
{
    DynamicFilterUtil.RegisterDynamicTypes(modelBuilder, typeof(MyContext), typeof(DynamicFiltersEnum),
        Database.Connection.ConnectionString);
}

// 4. Render the form from the view's fields
var filter = new DynamicFilterLogic("CustomerFilterView");
DynamicFilterConfig form = filter.GetDynamicFilterConfig(ctx);   // ColName, FieldDataType, FieldType per column

// 5. Apply posted values
filter.DynamicFilterFieldsMultiSelect["City"] = new List<string> { "Vilnius", "Kaunas" };
filter.DynamicFilterFields["TotalAmount" + DynamicFilterFieldConfig.SuffixFrom] = "100";

var expr = filter.FilterExprIntId<Customer>(ctx);              // null when nothing entered
var customers = expr == null ? ctx.Customers : ctx.Customers.Where(expr);
```

## Sample

`DynamicFilteringTest` is a console app that runs against LocalDB (`(LocalDB)\MSSQLLocalDB`, database `DynamicFilterTest`, see `App.config`). It creates the tables, two views and seed data, stores field overrides in a table, and prints the generated filter forms, the filtered results and the SQL.

```
dotnet build DynamicFiltering.sln
DynamicFilteringTest\bin\Debug\net472\DynamicFilteringTest.exe
```

## Requirements and limitations

- .NET Framework 4.7.2+, Entity Framework 6, SQL Server.
- Each view must return an `Id` column, or `Id1`/`Id2` for composite keys, matching the filtered entity's key.
- Column names with spaces are skipped.
- Decimal input accepts both `,` and `.` as the decimal separator. Dates are parsed with the current culture.
- Registered view types live in static state, one set per `DbContext` type per AppDomain.

## License

MIT, see [LICENSE](LICENSE).
