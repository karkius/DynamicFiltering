using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace DynamicFiltering
{
    /// <summary>
    /// Emits a POCO type at runtime whose properties mirror the columns of a SQL query result (a view),
    /// so EF6 can map it as an entity.
    /// </summary>
    internal static class DynamicTypeBuilder
    {
        private static void createAutoImplementedProperty(TypeBuilder builder, PropertyParameters propertyParams)
        {
            string propertyName = propertyParams.PropName;
            Type propertyType = propertyParams.Type;

            const string PrivateFieldPrefix = "m_";
            const string GetterPrefix = "get_";
            const string SetterPrefix = "set_";

            // Generate the field.
            FieldBuilder fieldBuilder = builder.DefineField(
                string.Concat(PrivateFieldPrefix, propertyName),
                              propertyType, FieldAttributes.Private);

            // Generate the property
            PropertyBuilder propertyBuilder = builder.DefineProperty(propertyName, System.Reflection.PropertyAttributes.HasDefault, propertyType, null);

            // Property getter and setter attributes.
            MethodAttributes propertyMethodAttributes =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig;

            // Define the getter method.
            MethodBuilder getterMethod = builder.DefineMethod(
                string.Concat(GetterPrefix, propertyName),
                propertyMethodAttributes, propertyType, Type.EmptyTypes);

            // ldarg.0; ldfld _field; ret
            ILGenerator getterILCode = getterMethod.GetILGenerator();
            getterILCode.Emit(OpCodes.Ldarg_0);
            getterILCode.Emit(OpCodes.Ldfld, fieldBuilder);
            getterILCode.Emit(OpCodes.Ret);

            // Define the setter method.
            MethodBuilder setterMethod = builder.DefineMethod(
                string.Concat(SetterPrefix, propertyName),
                propertyMethodAttributes, null, new Type[] { propertyType });

            // ldarg.0; ldarg.1; stfld _field; ret
            ILGenerator setterILCode = setterMethod.GetILGenerator();
            setterILCode.Emit(OpCodes.Ldarg_0);
            setterILCode.Emit(OpCodes.Ldarg_1);
            setterILCode.Emit(OpCodes.Stfld, fieldBuilder);
            setterILCode.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterMethod);
            propertyBuilder.SetSetMethod(setterMethod);
        }

        private static TypeBuilder createTypeBuilder(
            string assemblyName, string moduleName, string typeName, Type baseType = null)
        {
            var moduleBuilder = AppDomain
                .CurrentDomain
                .DefineDynamicAssembly(new AssemblyName(assemblyName),
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule(moduleName);
            TypeBuilder typeBuilder;
            if (baseType != null)
                typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, baseType);
            else
                typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            return typeBuilder;
        }

        /// <summary>
        /// Reads a connection string by name from the current app/web.config.
        /// </summary>
        private static string GetConnectionString(string connName)
        {
            try
            {
                var conf = AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE");
                var constr =
                    XElement.Load((string)conf)
                        .Elements("connectionStrings")
                        .Elements("add")
                        .FirstOrDefault(x => (string)x.Attribute("name") == connName);
                string value = (string)constr?.Attribute("connectionString");
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                DynamicFilterUtil.Log($"Could not read connection string '{connName}' from config: {ex.Message}");
            }
            return null;
        }

        private static void AddTableAttr(TypeBuilder builder, string tableName)
        {
            Type[] constructorParameters = new Type[] { typeof(string) };
            ConstructorInfo constructorInfo = typeof(TableAttribute).GetConstructor(constructorParameters);
            CustomAttributeBuilder tableAttributeBuilder = new CustomAttributeBuilder(constructorInfo,
                new object[] { tableName });
            builder.SetCustomAttribute(tableAttributeBuilder);
        }

        /// <summary>
        /// Builds a type deriving from <paramref name="baseTypeOn"/> with one property per result column of <paramref name="sql"/>
        /// (columns already present on the base type are skipped). Returns null when the query fails (e.g. the view does not exist).
        /// </summary>
        public static Type BuildTypeByTable(string uniqueTypeName, Type baseTypeOn, string sql, SqlParameter[] parameters, string connName, string connectionString = null)
        {
            Type baseRowType = baseTypeOn;

            TypeBuilder builder = createTypeBuilder("DynamicFilteringAssembly", "DynamicFilteringModule", "DynamicFilterType" + uniqueTypeName, baseRowType);
            AddTableAttr(builder, uniqueTypeName);

            using (SqlConnection connection = new SqlConnection(connectionString ?? GetConnectionString(connName)))
            {
                using (System.Data.IDbCommand command = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        command.CommandText = sql;
                        command.CommandTimeout = Math.Max(command.Connection.ConnectionTimeout, 120);
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.Add(param);
                            }
                        }
                        // SchemaOnly: we only need column names and types, not data
                        using (System.Data.IDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly))
                        {
                            var schema = reader.GetSchemaTable();

                            foreach (System.Data.DataRow row in schema.Rows)
                            {
                                string name = (string)row["ColumnName"];
                                if (name.Contains(" "))
                                {
                                    DynamicFilterUtil.Log(uniqueTypeName + " has an unsupported column name (contains spaces): [" + name + "]");
                                    continue;
                                }
                                if (baseRowType != null && baseRowType.GetProperties().Any(p => p.Name.ToLower() == name.ToLower()))
                                    continue;

                                Type type = (Type)row["DataType"];
                                if (type.IsValueType && (bool)row["AllowDBNull"])
                                {
                                    type = typeof(Nullable<>).MakeGenericType(type);
                                }
                                createAutoImplementedProperty(builder,
                                    new PropertyParameters
                                    {
                                        PropName = name,
                                        Type = type,
                                    });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        DynamicFilterUtil.Log($"Dynamic filter view {uniqueTypeName} skipped: {ex.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        DynamicFilterUtil.Log("BuildTypeByTable error: " + ex);
                        throw;
                    }
                    finally
                    {
                        command.Parameters.Clear();
                    }
                }
            }

            return builder.CreateType();
        }
    }
}
