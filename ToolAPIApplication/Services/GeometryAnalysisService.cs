using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.core;
using ToolAPIApplication.vo;
using ToolAPIApplication.enums;
using ToolAPIApplication.Utils;

namespace ToolAPIApplication.Services
{
    public class GeometryAnalysisService : IGeometryAnalysisService
    {
        private readonly IMongoService _mongoService;
        public GeometryAnalysisService(IMongoService mongoService)
        {
            _mongoService = mongoService ??
               throw new ArgumentNullException(nameof(mongoService));
        }

        //火球
        public double GetFireBallRadius(double equivalent_kt,double alt_ft)
        {
            MyAnalyse myAnalyse = new MyAnalyse();
            return myAnalyse.CalcfireBallRadius(equivalent_kt, alt_ft > 0);
        }

        // 冲击波
        public DamageResultVO GetShockWaveRadius(NbombBO bo)
        {
            // 传入的是吨，要变成千吨；输入的是米：要变成：英尺

            var rule = _mongoService.QueryRule("冲击波");
            if (rule != null)
            {
                MyAnalyse myAnalyse = new MyAnalyse();
                double radius =  myAnalyse.CalcShockWaveRadius(bo.Yield /= 1000, bo.Alt * Utils.Const.M2FT, rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2), 
                    bo.Lon,bo.Lat, bo.Alt,rule.limits, rule.unit);
            }
            return null;
        }
            
        // 核辐射
        public DamageResultVO GetNuclearRadiationRadius(NbombBO bo)
        {
            // 传入的是吨，要变成千吨；输入的是米：要变成：英尺

            var rule = _mongoService.QueryRule("早期核辐射");
            if (rule != null)
            {
                MyAnalyse myAnalyse = new MyAnalyse();
                double radius = myAnalyse.CalcNuclearRadiationRadius(bo.Yield /= 1000, bo.Alt * Utils.Const.M2FT, rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2),
                    bo.Lon, bo.Lat, bo.Alt, rule.limits, rule.unit);
            }
            return null;
        }
        //光辐射
        public DamageResultVO GetThermalRadiationRadius(NbombBO bo)
        {
            // 传入的是吨，要变成千吨；输入的是米：要变成：英尺

            var rule = _mongoService.QueryRule("光辐射");
            if (rule != null)
            {
                MyAnalyse myAnalyse = new MyAnalyse();
                double radius = myAnalyse.GetThermalRadiationR(bo.Yield /= 1000, bo.Alt * Utils.Const.M2FT, rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2),
                        bo.Lon, bo.Lat, bo.Alt, rule.limits, rule.unit);
            }
            return null;
        }
        //核电磁脉冲
        public DamageResultVO GetNuclearPulseRadius(NbombBO bo)
        {
            // 传入的是吨，不用变；输入的是米：要变成：千米
            var rule = _mongoService.QueryRule("核电磁脉冲");
            if (rule != null)
            {
                MyAnalyse myAnalyse = new MyAnalyse();
                double radius = myAnalyse.CalcNuclearPulseRadius(bo.Yield, bo.Alt / 1000, rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius*1000, 2),
                        bo.Lon, bo.Lat, bo.Alt, rule.limits, rule.unit);
            }
            return null;
        }
        

        public FalloutResultVO GetFalloutGeometryJson(NbombBO bo,double wind_speed, double wind_dir)
        {
            // 传入的是吨，要变成千吨; 输入的是米：要变成：英尺

            MyAnalyse myAnalyse = new MyAnalyse();
            double maximumDownwindDistance = 0;
            double maximumWidth = 0;
            List<Coor> coors = myAnalyse.CalcRadioactiveFalloutRegion(
                bo.Lon, bo.Lat, bo.Alt*Utils.Const.M2FT, bo.Yield/1000, wind_speed, wind_dir, DamageEnumeration.Light, ref maximumDownwindDistance, ref maximumWidth);

            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < coors.Count; i++)
            {
                coordinates.Add(new Coordinate(Math.Round(coors[i].lng,2), Math.Round(coors[i].lat,2)));
            }

            // 把coordinators 转换成geometry
            Coordinate[] coords = coordinates.ToArray();
            Polygon polygon = new NetTopologySuite.Geometries.Polygon(
                new LinearRing(coords));

            string geometry =  Translate.Geometry2GeoJson(polygon);

            return new FalloutResultVO(bo.nuclearExplosionID, geometry, 1, 1, "rads");

        }
    }
        
}
