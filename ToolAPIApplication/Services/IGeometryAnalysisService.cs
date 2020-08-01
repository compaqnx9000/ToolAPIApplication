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
        DamageResultVO GetFireBallRadius(NbombBO bo);
        DamageResultVO Nuclearradiation(NbombBO bo);
        DamageResultVO ShockWave(NbombBO bo);
        DamageResultVO ThermalRadiation(NbombBO bo);
        DamageResultVO GetNuclearPulseRadius(NbombBO bo);
        FalloutResultVO GetFalloutGeometryJson(NbombBO bo, double wind_speed, double wind_dir, int radshour);

       

    }
}
