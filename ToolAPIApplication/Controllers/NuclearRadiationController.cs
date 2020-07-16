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
    public class NuclearRadiationController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        public NuclearRadiationController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls> options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("nuclearradiation")]
        public IActionResult Nuclearradiation([FromBody] NbombBO bo)
        {
            if (bo.Yield <= 0 || bo.Yield/1000 > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            var result = _geometryAnalysisService.GetNuclearRadiationRadius(bo);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = result
            });
        }
    }
}
