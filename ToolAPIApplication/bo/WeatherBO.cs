using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.bo
{
    public class WeatherBO
    {
        public WeatherBO(double lon, double lat, double alt, double impactTimeUtc)
        {
            this.lon = lon;
            this.lat = lat;
            this.alt = alt;
            ImpactTimeUtc = impactTimeUtc;
        }

        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }
        public double ImpactTimeUtc { get; set; }
    }
}
