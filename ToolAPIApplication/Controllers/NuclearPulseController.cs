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
    public class NuclearPulseController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;

        public NuclearPulseController(IGeometryAnalysisService geometryAnalysisService)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

        }

        [HttpPost("nuclearpulse")]
        public IActionResult Nuclearpulse([FromBody] NbombBO bo)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _geometryAnalysisService.GetNuclearPulseRadius(bo)
            });
        }
    }
}
