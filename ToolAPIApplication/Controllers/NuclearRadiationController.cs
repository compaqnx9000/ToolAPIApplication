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

        public NuclearRadiationController(IGeometryAnalysisService geometryAnalysisService)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));
        }

        [HttpPost("nuclearradiation")]
        public IActionResult Nuclearradiation([FromBody] NbombBO bo)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = _geometryAnalysisService.Nuclearradiation(bo)
            });
        }
    }
}
