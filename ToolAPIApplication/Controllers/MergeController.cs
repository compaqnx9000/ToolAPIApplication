using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToolAPIApplication.bo;
using ToolAPIApplication.vo;
using ToolAPIApplication.Services;
using MyCore;
using Microsoft.Extensions.Configuration;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class MergeController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;


        public MergeController(
            IGeometryAnalysisService geometryAnalysisService)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

        }
        //纯是为了测试而生
        [HttpGet("merge")]
        public IActionResult MergeGet(string nuclearExplosionID, string OccurTime, double Lon, double Lat, double Alt,double Yield)
        {
            List<String> errors = new List<string>();

            if (nuclearExplosionID == null)
                errors.Add("nuclearExplosionID不能为空");
            if (OccurTime == null)
                errors.Add("OccurTime不能为空");
            if (Lon>180 || Lon<-180)
                errors.Add("Lon必须介于-180~180");
            if (Lat > 90 || Lat < -90)
                errors.Add("Lon必须介于-90~90");
            if (Alt  < 0)
                errors.Add("Lon必须大于等于0");
            if (Yield <= 0)
                errors.Add("Yield必须大于0");
            if (errors.Count > 0)
            {
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = errors,
                    return_data = ""
                });
            }
           
            NbombBO bo = new NbombBO(nuclearExplosionID, OccurTime, Lon, Lat, Alt,Yield);
            var fireball = _geometryAnalysisService.GetFireBallRadius(bo);
            var nuclearradiation = _geometryAnalysisService.Nuclearradiation(bo);
            var airblast = _geometryAnalysisService.ShockWave(bo);
            var thermalradiation = _geometryAnalysisService.ThermalRadiation(bo);
            var nuclearpulse = _geometryAnalysisService.GetNuclearPulseRadius(bo);


            List<MergeVO> list = new List<MergeVO>();
            list.Add(new MergeVO("核火球",
                new DamageResultVO(bo.nuclearExplosionID, fireball.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), 0, "")));
            list.Add(new MergeVO("早期核辐射",
                new DamageResultVO(bo.nuclearExplosionID, nuclearradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), nuclearradiation.value, nuclearradiation.unit)));
            list.Add(new MergeVO("冲击波",
                new DamageResultVO(bo.nuclearExplosionID, airblast.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), airblast.value, airblast.unit)));
            list.Add(new MergeVO("光辐射",
                new DamageResultVO(bo.nuclearExplosionID, thermalradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), thermalradiation.value, thermalradiation.unit)));
            list.Add(new MergeVO("核电磁脉冲",
                new DamageResultVO(bo.nuclearExplosionID, nuclearpulse.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), nuclearpulse.value, nuclearpulse.unit)));

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = list
            });
        }

        [HttpPost("merge")]
        public IActionResult Merge([FromBody] NbombBO bo)
        {
            var fireball            = _geometryAnalysisService.GetFireBallRadius(bo);
            var nuclearradiation    = _geometryAnalysisService.Nuclearradiation(bo);
            var airblast            = _geometryAnalysisService.ShockWave(bo);
            var thermalradiation    = _geometryAnalysisService.ThermalRadiation(bo);
            var nuclearpulse        = _geometryAnalysisService.GetNuclearPulseRadius(bo);
            

            List<MergeVO> list = new List<MergeVO>();
            list.Add(new MergeVO("核火球", 
                new DamageResultVO(bo.nuclearExplosionID, fireball.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), 0, "")));
            list.Add(new MergeVO("早期核辐射", 
                new DamageResultVO(bo.nuclearExplosionID, nuclearradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), nuclearradiation.value, nuclearradiation.unit)));
            list.Add(new MergeVO("冲击波", 
                new DamageResultVO(bo.nuclearExplosionID, airblast.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), airblast.value, airblast.unit)));
            list.Add(new MergeVO("光辐射",
                new DamageResultVO(bo.nuclearExplosionID, thermalradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), thermalradiation.value, thermalradiation.unit)));
            list.Add(new MergeVO("核电磁脉冲", 
                new DamageResultVO(bo.nuclearExplosionID, nuclearpulse.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), nuclearpulse.value, nuclearpulse.unit)));

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = list
            });
        }


        [HttpPost("area")]
        public IActionResult Area([FromBody] NbombBO bo)
        {
            if (bo.Yield <= 0 || bo.Yield / 1000 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "当量必须大于0并且小于100000千吨",
                    return_data = ""
                });

            //var fireball = _geometryAnalysisService.GetFireBallRadius(bo);
            //var nuclearradiation = _geometryAnalysisService.Nuclearradiation(bo);
            //var airblast = _geometryAnalysisService.ShockWave(bo);
            //var thermalradiation = _geometryAnalysisService.ThermalRadiation(bo);
            var nuclearpulse = _geometryAnalysisService.GetNuclearPulseRadius(bo);

           
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    damageRadius= nuclearpulse.DamageRadius,
                    lon =  bo.Lon,
                    lat = bo.Lat,
                    alt =  bo.Alt
                }
            });
        }








    }
}
