using MyCore;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.vo;

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
        public DamageResultVO GetFireBallRadius(NbombBO bo)
        {
            double radius = MyCore.NuclearAlgorithm.GetFireBallRadius(bo.Yield.GetValueOrDefault(), bo.Alt.GetValueOrDefault());
            return new DamageResultVO(bo.nuclearExplosionID,
                Math.Round(radius, 2), 
                bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), 0, "");
        }

        // 冲击波
        public DamageResultVO ShockWave(NbombBO bo)
        {
            var rule = _mongoService.QueryRule("冲击波");
            if (rule != null)
            {
                double radius = MyCore.NuclearAlgorithm.GetShockWaveRadius(bo.Yield.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2), 
                    bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits, rule.unit);
            }
            return null;
        }
            
        // 核辐射
        public DamageResultVO Nuclearradiation(NbombBO bo)
        {
            var rule = _mongoService.QueryRule("早期核辐射");
            if (rule != null)
            {
                double radius = MyCore.NuclearAlgorithm.GetNuclearRadiationRadius(bo.Yield.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits);

                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2),
                    bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits, rule.unit);
            }
            return null;
        }
        //光辐射
        public DamageResultVO ThermalRadiation(NbombBO bo)
        {
            var rule = _mongoService.QueryRule("光辐射");
            if (rule != null)
            {
                double radius = MyCore.NuclearAlgorithm.GetThermalRadiationRadius(bo.Yield.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits); 
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2),
                        bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits, rule.unit);
            }
            return null;
        }
        //核电磁脉冲
        public DamageResultVO GetNuclearPulseRadius(NbombBO bo)
        {
            var rule = _mongoService.QueryRule("核电磁脉冲");
            if (rule != null)
            {
                double radius = MyCore.NuclearAlgorithm.GetNuclearPulseRadius(bo.Yield.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits);
                return new DamageResultVO(bo.nuclearExplosionID, Math.Round(radius, 2),
                        bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), rule.limits, rule.unit);
            }
            return null;
        }
        

        public FalloutResultVO GetFalloutGeometryJson(NbombBO bo,double wind_speed, double wind_dir, int radshour)
        {
            var geometry = MyCore.NuclearAlgorithm.GetFalloutGeometryJson(bo.Yield.GetValueOrDefault(), bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(),
            bo.Alt.GetValueOrDefault(), wind_speed, wind_dir, radshour);
            return new FalloutResultVO(bo.nuclearExplosionID, geometry, 1, 1, "rads/h");

        }
    }
        
}
