using DynamicFiltering;
using DynamicFiltering.Config;
using DynamicFilteringTest.App;
using DynamicFilteringTest.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DynamicFilteringTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 0. Sample only: create the database, tables, views and seed data.
            DatabaseSetup.EnsureCreated(ConfigurationManager.ConnectionStrings["DFContext"].ConnectionString);

            // 1. Once at application start, before the first DbContext.
            DynamicFilterUtil.AppStart(DynamicFilterService.GetConfigOverride, msg => Console.WriteLine("  [DynamicFiltering] " + msg));

            using (var ctx = new DFContext())
            {
                // 2. Ask which fields each filter has - this is what a UI renders as the filter form.
                foreach (DynamicFiltersEnum filter in Enum.GetValues(typeof(DynamicFiltersEnum)))
                {
                    PrintForm(filter, DynamicFilterService.GetFormConfig(filter, ctx));
                }

                // 3. Values as they would be posted from the form.
                //    Multiselect: City in (Vilnius, Kaunas); range: TotalAmount >= 100.
                var filter1 = DynamicFilterService.Create(DynamicFiltersEnum.Customers);
                filter1.DynamicFilterFieldsMultiSelect["City"] = new List<string> { "Vilnius", "Kaunas" };
                filter1.DynamicFilterFields["TotalAmount" + DynamicFilterFieldConfig.SuffixFrom] = "100";

                // Usage A: a Where expression added to any existing query of the entity.
                IQueryable<Customer> baseQuery = ctx.Customers.Where(c => c.Id > 0);
                var expr = filter1.FilterExprIntId<Customer>(ctx);
                var queryA = expr == null ? baseQuery : baseQuery.Where(expr);
                Print("City in (Vilnius, Kaunas) AND TotalAmount >= 100", queryA);
                Console.WriteLine("  SQL sent to the server (a single query):");
                Console.WriteLine("  " + queryA.ToString().Replace(Environment.NewLine, Environment.NewLine + "  "));

                // Usage B: a join. Range with a date and a decimal written with a comma, plus a bool.
                var filter2 = DynamicFilterService.Create(DynamicFiltersEnum.Customers);
                filter2.DynamicFilterFields["LastOrderDate" + DynamicFilterFieldConfig.SuffixTo] = "2024-01-01";
                filter2.DynamicFilterFields["TotalAmount" + DynamicFilterFieldConfig.SuffixFrom] = "15,5";
                filter2.DynamicFilterFields["IsVip"] = "1";
                var queryB = filter2.FilteredQuery<Customer, int>(ctx, ctx.Customers) ?? ctx.Customers;
                Print("LastOrderDate <= 2024-01-01 AND TotalAmount >= 15,5 AND IsVip", queryB);

                // Name is overridden in the database to compare with "=" instead of Contains.
                var filter3 = DynamicFilterService.Create(DynamicFiltersEnum.Customers);
                filter3.DynamicFilterFields["Name"] = "Ona";
                Print("Name = 'Ona' (exact match override, so no rows)", filter3.FilteredQuery<Customer, int>(ctx, ctx.Customers) ?? ctx.Customers);
                filter3.DynamicFilterFields["Name"] = "Ona Onaitė";
                Print("Name = 'Ona Onaitė'", filter3.FilteredQuery<Customer, int>(ctx, ctx.Customers) ?? ctx.Customers);

                // No values entered: nothing to filter by, the library returns null and the base query is used as is.
                var empty = DynamicFilterService.Create(DynamicFiltersEnum.Customers);
                Console.WriteLine();
                Console.WriteLine($"Empty filter: HasValues={empty.HasValues()}, expression is null: {empty.FilterExprIntId<Customer>(ctx) == null}");
            }

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
        }

        private static void PrintForm(DynamicFiltersEnum filter, DynamicFilterConfigVM form)
        {
            Console.WriteLine();
            Console.WriteLine($"Filter form for {filter} ({DynamicFilterService.ViewName(filter)}):");
            if (form == null)
            {
                Console.WriteLine("  (view not available - filter disabled)");
                return;
            }
            foreach (var f in form.Fields)
            {
                Console.WriteLine($"  {f.DisplayName,-16} {f.ColName,-14} {f.DataType.Name,-9} {f.FieldType}");
            }
        }

        private static void Print(string title, IQueryable<Customer> query)
        {
            Console.WriteLine();
            Console.WriteLine(title + ":");
            var rows = query.OrderBy(c => c.Id).ToList();
            if (!rows.Any()) Console.WriteLine("  (none)");
            foreach (var c in rows)
            {
                Console.WriteLine($"  {c.Id}: {c.Name}");
            }
        }
    }
}
