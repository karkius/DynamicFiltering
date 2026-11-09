using DynamicFiltering.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Config
{
    public class DynamicFilterViewConfigAttribute : Attribute
    {
        /// <summary>
        /// Id1 name - type,
        /// Id2 name - type...
        /// </summary>
        //public Dictionary<string, Type> IdTypes { get; set; }

        public string ViewName { get; }
        public Type BaseType { get; }
        //public Type FilteredEntityType { get; }
        public DynamicFilterViewConfigAttribute(string ViewName, Type BaseType//, Type FilteredEntityType
            )
        {
            this.ViewName = ViewName;
            this.BaseType = BaseType;
            //this.FilteredEntityType = FilteredEntityType;

            //this.IdTypes = FilteredEntityType.GetField("DynamicFilterIdTypes")?.GetValue(null) as Dictionary<string, Type>;

        }

        //internal string GetIdSelectExpr()
        //{
        //    if (IdTypes == null)
        //    {
        //        return "Id";
        //    }
        //    if (IdTypes.Count == 1)
        //    {
        //        return IdTypes.First().Key;
        //    }
        //    if (IdTypes.Count > 1)
        //    {
        //        string rez = "new (";
        //        int idCount = 1;
        //        foreach (var item in IdTypes)
        //        {
        //            if (idCount != 1)
        //            {
        //                rez += ",";
        //            }
        //            rez += item.Key + " as Id" + idCount;
        //            idCount++;
        //        }
        //        rez += ")";
        //        return rez;
        //    }
        //    throw new DynamicFilterException("IdTypes empty");

        //}

    }
}
