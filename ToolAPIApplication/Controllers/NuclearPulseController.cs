using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.enums;
using ToolAPIApplication.Services;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class NuclearPulseController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        public NuclearPulseController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls> options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("nuclearpulse")]
        public IActionResult Nuclearpulse([FromBody] NbombBO bo)
        {
            var result = _geometryAnalysisService.GetNuclearPulseRadius(bo);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = result
            });
        }
    }
}
