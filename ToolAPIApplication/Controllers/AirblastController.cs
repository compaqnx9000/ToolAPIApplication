using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.Services;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class AirblastController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        public AirblastController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls> options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("airblast")]
        public IActionResult Airblast([FromBody] NbombBO bo)
        {
            if (bo.Yield <= 0 || bo.Yield/1000 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "equivalent/1000 must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });


            var result = _geometryAnalysisService.GetShockWaveRadius(bo);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = result
            });
        }
    }
}
