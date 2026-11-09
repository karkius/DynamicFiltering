using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Config
{
    public class DynamicFilterFieldConfig //: IDynamicFilterFieldConfig
    {
        public DynamicFilterFieldConfig(string colname, Type fieldDataType)
        {
            ColName = colname;
            FieldDataType = fieldDataType;
            FieldType = DefaultFieldType;
            StringValueTestOperator = ValueTestOperatorsEnum.Contains;
        }

        //public string ViewName { get; set; }
        public string ColName { get; set; }
        public ValueTestOperatorsEnum StringValueTestOperator { get; set; }

        public DynamicFilterFieldTypes FieldType { get; set; }
        public Type FieldDataType { get; set; }

        internal DynamicFilterFieldTypes DefaultFieldType
        {
            get
            {
                if (FieldDataType == null)
                    return DynamicFilterFieldTypes.Single;
                var genericArgs = FieldDataType.GetGenericArguments();
                Type realType = FieldDataType;
                if (genericArgs.Any())
                    realType = genericArgs[0];

                return DefaultFieldTypeByDataType.TryGetValue(realType, out var fieldType) ? fieldType : DynamicFilterFieldTypes.Single;
            }
        }


        /// <summary>
        /// Marks range bounds in posted field keys ("Col_#@#From" -> ">=", "Col_#@#To" -> "<="). A plain "From" suffix could clash with a real column name.
        /// </summary>
        static string UnrepeatableToken { get; } = "_#@#";


        public static string SuffixFrom { get; } = UnrepeatableToken + "From";
        public static string SuffixTo { get; } = UnrepeatableToken + "To";


        public static Dictionary<Type, DynamicFilterFieldTypes> DefaultFieldTypeByDataType { get; }
            = new Dictionary<Type, DynamicFilterFieldTypes>{
            { typeof(DateTime), DynamicFilterFieldTypes.Range },
            { typeof(Int64), DynamicFilterFieldTypes.Range },
            { typeof(Int32), DynamicFilterFieldTypes.Range },
            { typeof(string), DynamicFilterFieldTypes.Single },
            { typeof(Decimal), DynamicFilterFieldTypes.Range },
            { typeof(bool), DynamicFilterFieldTypes.Single}

        };
    }
}
