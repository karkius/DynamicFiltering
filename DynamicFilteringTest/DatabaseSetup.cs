using System.Data.SqlClient;

namespace DynamicFilteringTest
{
    /// <summary>
    /// Creates the sample database with plain SQL.
    /// The views must exist before the first DbContext is created, because the dynamic types are built from them in OnModelCreating.
    /// In a real application the views are part of your database schema (migrations / deployment scripts).
    /// </summary>
    public static class DatabaseSetup
    {
        public static void EnsureCreated(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string database = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using (var master = new SqlConnection(builder.ConnectionString))
            {
                master.Open();
                Execute(master, $"IF DB_ID(N'{database}') IS NULL CREATE DATABASE [{database}]");
            }

            using (var db = new SqlConnection(connectionString))
            {
                db.Open();
                foreach (var sql in Script)
                {
                    Execute(db, sql);
                }
            }
        }

        private static void Execute(SqlConnection connection, string sql)
        {
            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static readonly string[] Script =
        {
            @"IF OBJECT_ID('DynamicFilterTest_Customer') IS NULL
              CREATE TABLE DynamicFilterTest_Customer (
                  Id   int IDENTITY PRIMARY KEY,
                  Name nvarchar(200) NOT NULL,
                  City nvarchar(100) NULL,
                  RegisteredOn date NOT NULL,
                  IsVip bit NOT NULL)",

            @"IF OBJECT_ID('DynamicFilterTest_Order') IS NULL
              CREATE TABLE DynamicFilterTest_Order (
                  Id         int IDENTITY PRIMARY KEY,
                  CustomerId int NOT NULL REFERENCES DynamicFilterTest_Customer(Id),
                  OrderDate  date NOT NULL,
                  Amount     decimal(18,2) NOT NULL)",

            @"IF OBJECT_ID('DynamicFilterConfigOverrideEntities') IS NULL
              CREATE TABLE DynamicFilterConfigOverrideEntities (
                  Type int NOT NULL,
                  ColName nvarchar(128) NOT NULL,
                  DisplayName nvarchar(200) NULL,
                  FieldType int NULL,
                  StringValueTestOperator int NULL,
                  [Order] int NULL,
                  PRIMARY KEY (Type, ColName))",

            // View 1: customer columns + order aggregates. Each column becomes a filter field.
            @"CREATE OR ALTER VIEW DynamicFilterTest_CustomerView AS
              SELECT c.Id, c.Name, c.City, c.RegisteredOn, c.IsVip,
                     COUNT(o.Id)      AS OrderCount,
                     SUM(o.Amount)    AS TotalAmount,
                     MAX(o.OrderDate) AS LastOrderDate
              FROM DynamicFilterTest_Customer c
              LEFT JOIN DynamicFilterTest_Order o ON o.CustomerId = c.Id
              GROUP BY c.Id, c.Name, c.City, c.RegisteredOn, c.IsVip",

            // View 2: a different set of fields for another list. Columns with spaces are skipped (and logged).
            @"CREATE OR ALTER VIEW DynamicFilterTest_OrderView AS
              SELECT o.Id, o.OrderDate, o.Amount, c.Name AS CustomerName, c.City AS CustomerCity,
                     o.Amount AS [Amount with spaces]
              FROM DynamicFilterTest_Order o
              JOIN DynamicFilterTest_Customer c ON c.Id = o.CustomerId",

            @"IF NOT EXISTS (SELECT 1 FROM DynamicFilterTest_Customer)
              BEGIN
                  INSERT DynamicFilterTest_Customer (Name, City, RegisteredOn, IsVip) VALUES
                      (N'Jonas Jonaitis',    N'Vilnius',  '2020-03-01', 1),
                      (N'Petras Petraitis',  N'Kaunas',   '2021-06-15', 0),
                      (N'Ona Onaitė',        N'Vilnius',  '2022-01-20', 0),
                      (N'Rūta Rūtaitė',      N'Klaipėda', '2023-09-05', 1),
                      (N'Tomas Tomaitis',    NULL,        '2024-02-11', 0);
                  INSERT DynamicFilterTest_Order (CustomerId, OrderDate, Amount) VALUES
                      (1, '2024-01-10', 120.50), (1, '2024-05-02', 80.00),
                      (2, '2023-11-11', 15.99),
                      (3, '2024-07-07', 300.00), (3, '2024-08-08', 45.10), (3, '2024-09-09', 10.00),
                      (4, '2022-12-24', 999.99);
              END",

            // Per-field overrides normally edited by an admin UI: make City a multiselect, make Name compare exactly.
            @"IF NOT EXISTS (SELECT 1 FROM DynamicFilterConfigOverrideEntities)
              INSERT DynamicFilterConfigOverrideEntities (Type, ColName, DisplayName, FieldType, StringValueTestOperator, [Order]) VALUES
                  (0, 'City',        N'City',            2,    NULL, 1),
                  (0, 'TotalAmount', N'Total ordered',   NULL, NULL, 2),
                  (0, 'Name',        N'Customer name',   NULL, 1,    0)",
        };
    }
}
