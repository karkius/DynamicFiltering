using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFiltering.Util
{
    public class DynamicFilterException:Exception
    {
        public DynamicFilterException(string msg):base(msg){ }
    }
}
