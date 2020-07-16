using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.vo;

namespace ToolAPIApplication.Services
{
    public interface IGeometryAnalysisService
    {
        /// <summary>
        /// 获取火球半径。
        /// </summary>
        /// <param name="equivalent_kt">当量（千吨）。</param>
        /// <param name="alt_ft">高度（英尺）</param>
        /// <returns></returns>
        double GetFireBallRadius(double equivalent_kt, double alt_ft);

        // 根据当量破坏级别，返回半径
        DamageResultVO GetShockWaveRadius(NbombBO bo);
        DamageResultVO GetNuclearRadiationRadius(NbombBO bo);
        DamageResultVO GetThermalRadiationRadius(NbombBO bo);
        DamageResultVO GetNuclearPulseRadius(NbombBO bo);
        FalloutResultVO GetFalloutGeometryJson(NbombBO bo, double wind_speed, double wind_dir);

       

    }
}
