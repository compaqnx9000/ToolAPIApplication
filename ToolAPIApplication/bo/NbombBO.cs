﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.bo
{
    public class NbombBO:ICloneable
    {
        public NbombBO()
        {

        }

        public NbombBO(string nuclearExplosionID, double damageRadius, string occurTime, double lon, double lat, double alt, double yield)
        {
            this.nuclearExplosionID = nuclearExplosionID;
            DamageRadius = damageRadius;
            OccurTime = occurTime;
            Lon = lon;
            Lat = lat;
            Alt = alt;
            Yield = yield;
        }

        public string nuclearExplosionID { get; set; }
        public double DamageRadius { get; set; }
        public string OccurTime { get; set; }
        [Required(ErrorMessage = "Lon不能为空")]
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double Alt { get; set; }
        [Required(ErrorMessage = "当量不能为空")]
        public double Yield { get; set; }

        public object Clone()
        {
            return new NbombBO(this.nuclearExplosionID, this.DamageRadius, this.OccurTime, this.Lon, this.Lat, this.Alt, this.Yield);

        }
    }
}
