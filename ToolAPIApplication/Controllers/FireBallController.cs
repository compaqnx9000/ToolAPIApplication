using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using ToolAPIApplication.bo;
using ToolAPIApplication.Services;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class FireBallController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;

        public FireBallController(IGeometryAnalysisService geometryAnalysisService)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

        }

        [HttpPost("fireball")]
        public IActionResult Fireball([FromBody] NbombBO bo)
        {
            if (bo.Yield <= 0 || bo.Yield / 1000.0 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "当量必须大于0并且小于100000千吨",
                    return_data = ""
                });


            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _geometryAnalysisService.GetFireBallRadius(bo)
            });
        }
    }
}
