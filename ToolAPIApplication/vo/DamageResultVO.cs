using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.vo
{
    public class DamageResultVO
    {
        public string nuclearExplosionID { get; set; }
        public double DamageRadius { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }
        public double value { get; set; }
        public string unit { get; set; }

        public DamageResultVO(string nuclearExplosionID, double damageRadius, double lon, 
                                double lat, double alt,double value, string unit)
        {
            this.nuclearExplosionID = nuclearExplosionID ?? throw new ArgumentNullException(nameof(nuclearExplosionID));
            DamageRadius = damageRadius;
            this.lon = lon;
            this.lat = lat;
            this.alt = alt;
            this.value = value;
            this.unit = unit;
        }
    }
}
