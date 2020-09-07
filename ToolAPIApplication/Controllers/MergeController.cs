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

        [HttpPost("merge")]
        public IActionResult Merge([FromBody] NbombBO bo)
        {
            var fireball            = _geometryAnalysisService.GetFireBallRadius(bo);
            var nuclearradiation    = _geometryAnalysisService.Nuclearradiation(bo);
            var airblast            = _geometryAnalysisService.ShockWave(bo);
            var thermalradiation    = _geometryAnalysisService.ThermalRadiation(bo);
            var nuclearpulse        = _geometryAnalysisService.GetNuclearPulseRadius(bo);
            

            List<MergeDTO> list = new List<MergeDTO>();
            list.Add(new MergeDTO("核火球", 
                new DamageResultVO(bo.nuclearExplosionID, fireball.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), 0, "")));
            list.Add(new MergeDTO("早期核辐射", 
                new DamageResultVO(bo.nuclearExplosionID, nuclearradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), nuclearradiation.value, nuclearradiation.unit)));
            list.Add(new MergeDTO("冲击波", 
                new DamageResultVO(bo.nuclearExplosionID, airblast.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), airblast.value, airblast.unit)));
            list.Add(new MergeDTO("光辐射",
                new DamageResultVO(bo.nuclearExplosionID, thermalradiation.DamageRadius, bo.Lon.GetValueOrDefault(), bo.Lat.GetValueOrDefault(), bo.Alt.GetValueOrDefault(), thermalradiation.value, thermalradiation.unit)));
            list.Add(new MergeDTO("核电磁脉冲", 
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
