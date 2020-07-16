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

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class MergeController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;


        public MergeController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls>  options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("merge")]
        public IActionResult Merge([FromBody] NbombBO bo)
        {
           

            if (bo.Yield <= 0 || bo.Yield/1000 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.Yield must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            if (bo.nuclearExplosionID == null)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.nuclearExplosionID cannot be empty",
                    return_data = ""
                });

            double fireball_radius  = _geometryAnalysisService.GetFireBallRadius(bo.Yield/1000,bo.Alt* MyCore.Utils.Const.M2FT);
            var nuclearradiation    = _geometryAnalysisService.GetNuclearRadiationRadius(bo);
            var airblast            = _geometryAnalysisService.GetShockWaveRadius(bo);
            var thermalradiation    = _geometryAnalysisService.GetThermalRadiationRadius(bo);
            var nuclearpulse        = _geometryAnalysisService.GetNuclearPulseRadius(bo);
            

            List<MergeDTO> list = new List<MergeDTO>();
            list.Add(new MergeDTO("H火球", 
                new DamageResultVO(bo.nuclearExplosionID, fireball_radius, bo.Lon, bo.Lat, bo.Alt, 0, "")));
            list.Add(new MergeDTO("核辐射", 
                new DamageResultVO(bo.nuclearExplosionID, nuclearradiation.DamageRadius, bo.Lon, bo.Lat, bo.Alt, nuclearradiation.value, nuclearradiation.unit)));
            list.Add(new MergeDTO("冲击波", 
                new DamageResultVO(bo.nuclearExplosionID, airblast.DamageRadius, bo.Lon, bo.Lat, bo.Alt, airblast.value, airblast.unit)));
            list.Add(new MergeDTO("热/光辐射",
                new DamageResultVO(bo.nuclearExplosionID, thermalradiation.DamageRadius, bo.Lon, bo.Lat, bo.Alt, thermalradiation.value, thermalradiation.unit)));
            list.Add(new MergeDTO("电磁脉冲", 
                new DamageResultVO(bo.nuclearExplosionID, nuclearpulse.DamageRadius, bo.Lon, bo.Lat, bo.Alt, nuclearpulse.value, nuclearpulse.unit)));

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = list
            });
        }

        
   


      




    }
}
