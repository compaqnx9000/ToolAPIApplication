using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.dto
{
    public class DamageResultDTO
    {
        public string nuclearExplosionID { get; set; }
        public double DamageRadius { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }

        public DamageResultDTO(string nuclearExplosionID, double damageRadius, double lon, double lat, double alt)
        {
            this.nuclearExplosionID = nuclearExplosionID ?? throw new ArgumentNullException(nameof(nuclearExplosionID));
            DamageRadius = damageRadius;
            this.lon = lon;
            this.lat = lat;
            this.alt = alt;
        }
    }
}
