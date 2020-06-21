using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.dto;
using ToolAPIApplication.enums;

namespace ToolAPIApplication.Services
{
    public interface IGeometryAnalysisService
    {
        // 根据当量破坏级别，返回半径
        double GetShockWaveRadius(double equivalent, double ft, DamageEnumeration level);
        double GetNuclearRadiationRadius(double equivalent, double ft,DamageEnumeration level);
        double GetThermalRadiationRadius(double equivalent,double ft, DamageEnumeration level);
        double GetNuclearPulseRadius(double equivalent, double km,DamageEnumeration level);
        double GetFireBallRadius(NbombBO bo);
        string GetFalloutGeometryJson(NbombBO bo, double wind_speed, double wind_dir);

        // 根据当量破坏级别，返回geojson
        //string GetShockWaveGeometryJson(FactorDTO bo);
        //string GetNuclearRadiationGeometryJson(FactorDTO bo);
        //string GetThermalRadiationGeometryJson(FactorDTO bo);
        //string GetNuclearPulseGeometryJson(FactorDTO bo);

        // 根据当量破坏级别，返回geometry
        //Geometry GetShockWaveGeometry(FactorDTO bo, ref double r);
        //Geometry GetNuclearRadiationGeometry(FactorDTO bo, ref double r);
        //Geometry GetThermalRadiationGeometry(FactorDTO bo, ref double r);
        //Geometry GetRadiationPulseGeometry(FactorDTO bo, ref double r);
    }
}
