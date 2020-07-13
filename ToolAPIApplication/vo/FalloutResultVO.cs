using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.vo
{
    public class FalloutResultVO
    {
        public FalloutResultVO(string nuclearExplosionID, string damageGeometry, int radValue, double value, string unit)
        {
            this.nuclearExplosionID = nuclearExplosionID;
            this.damageGeometry = damageGeometry;
            this.radValue = radValue;
            this.value = value;
            this.unit = unit;
        }

        public string nuclearExplosionID { get; set; }
        public string damageGeometry { get; set; }
        public int radValue { get; set; }
        public double value { get; set; }
        public string unit { get; set; }
    }
}
