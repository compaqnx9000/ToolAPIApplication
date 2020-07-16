using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.Services;
using ToolAPIApplication.vo;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class FireBallController : ControllerBase
    {
        private readonly IMongoService _mongoService;
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        public FireBallController(IMongoService mongoService,
                                    IGeometryAnalysisService geometryAnalysisService,
                                    IOptions<ServiceUrls> options)
        {
            _mongoService = mongoService ??
                throw new ArgumentNullException(nameof(mongoService));

            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("fireball")]
        public IActionResult Fireball([FromBody] NbombBO bo)
        {
            if (bo.Yield <= 0 || bo.Yield / 1000 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            // 传入的是吨，要变成千吨; 输入的是米：要变成：英尺
            double radius = _geometryAnalysisService.GetFireBallRadius(bo.Yield/1000,bo.Alt* MyCore.Utils.Const.M2FT);


            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new DamageResultVO(
                    bo.nuclearExplosionID,
                    Math.Round(radius, 2),
                    bo.Lon,
                    bo.Lat,
                    bo.Alt,
                    0,
                    "")
            });
        }
    }
}
