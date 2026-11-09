using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering
{
    public interface IHasId : IHasId<int> { }

    public interface IHasId<TIdType>
    {
        TIdType Id { get; set; }
    }

    public interface IHasId<TIdType1, TIdType2>
    {
        TIdType1 Id1 { get; set; }
        TIdType2 Id2 { get; set; }
    }

    public class Ids2<TIdType1, TIdType2> : IHasId<TIdType1, TIdType2>
    {
        [Key]
        [Column(Order =1)]
        public TIdType1 Id1 { get; set; }
        [Key]
        [Column(Order =2)]
        public TIdType2 Id2 { get; set; }
    }

}
