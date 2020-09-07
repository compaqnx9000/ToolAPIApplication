using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.bo
{
    public class NbombBO : ICloneable
    {
        public NbombBO()
        {

        }

        public NbombBO(string nuclearExplosionID,  string occurTime, double lon, double lat, double alt, double yield)
        {
            this.nuclearExplosionID = nuclearExplosionID;
            OccurTime = occurTime;
            Lon = lon;
            Lat = lat;
            Alt = alt;
            Yield = yield;
        }

        [Required(ErrorMessage = "nuclearExplosionID不能为空")]
        public string nuclearExplosionID { get; set; }


        [Required(ErrorMessage = "OccurTime不能为空")]
        public string OccurTime { get; set; }

        [Required(ErrorMessage = "Lon不能为空")]
        public double? Lon { get; set; }

        [Required(ErrorMessage = "Lat不能为空")]
        public double? Lat { get; set; }

        [Required(ErrorMessage = "Alt不能为空")]
        public double? Alt { get; set; }

        [Range(1, 100000000, ErrorMessage = "Yield必须介于1~100000000之间")]
        [Required(ErrorMessage = "Yield不能为空")]
        public double? Yield { get; set; }

        public object Clone()
        {
            return new NbombBO(this.nuclearExplosionID, this.OccurTime, this.Lon.GetValueOrDefault(), 
                this.Lat.GetValueOrDefault(), this.Alt.GetValueOrDefault(), this.Yield.GetValueOrDefault());
        }
    }
}
