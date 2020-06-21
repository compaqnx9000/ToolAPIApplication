using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.core;
using ToolAPIApplication.dto;
using ToolAPIApplication.enums;
using ToolAPIApplication.Utils;

namespace ToolAPIApplication.Services
{
    public class GeometryAnalysisService : IGeometryAnalysisService
    {
        const double M2FT = 3.2808399;

        // 冲击波
        public double GetShockWaveRadius(double equivalent, double ft,DamageEnumeration level)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcShockWaveRadius(equivalent,ft,
                Utils.Helpers.Convert.ToPsi(level.GetHashCode()));
        }
        // 核辐射
        public double GetNuclearRadiationRadius(double equivalent, double ft,DamageEnumeration level)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcNuclearRadiationRadius(equivalent, ft,
                Utils.Helpers.Convert.ToRem(level.GetHashCode()));
        }
        //光辐射
        public double GetThermalRadiationRadius(double equivalent, double ft,DamageEnumeration level)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcThermalRadiationRadius(equivalent, ft, Utils.Helpers.Convert.ToThrem(level.GetHashCode()));
        }
        //核电磁脉冲
        public double GetNuclearPulseRadius(double equivalent, double km,DamageEnumeration level)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcNuclearPulseRadius(equivalent, km, Utils.Helpers.Convert.ToPluse(level.GetHashCode()));
        }
        //火球半径
        public double GetFireBallRadius(NbombBO bo)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcfireBallRadius(bo.Yield, bo.Alt > 0);
        }

        public string GetFalloutGeometryJson(NbombBO bo,double wind_speed, double wind_dir)
        {
            MyAnalyse myAnalyse = new MyAnalyse();

            List<Coor> coors = myAnalyse.CalcRadioactiveFalloutRegion(
                bo.Lon, bo.Lat, bo.Alt, bo.Yield, wind_speed, wind_dir, DamageEnumeration.Light);

            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < coors.Count; i++)
            {
                coordinates.Add(new Coordinate(Math.Round(coors[i].lng,2), Math.Round(coors[i].lat,2)));
            }

            // 把coordinators 转换成geometry
            Coordinate[] coords = coordinates.ToArray();
            Polygon polygon = new NetTopologySuite.Geometries.Polygon(
                new LinearRing(coords));

            return Translate.Geometry2GeoJson(polygon);

        }
    }
        
}
