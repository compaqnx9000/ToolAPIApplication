using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class MockController : ControllerBase
    {
        [HttpPost("weather")]
        public IActionResult Weather([FromBody] dynamic dto)
        {
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "查询成功",
                return_data = new
                {
                    season = 2,
                    wind_speed = 12.3,
                    wind_dir = 23.12,
                    temperature = 23.2,
                    humidity = 23.4,
                    rainfall = 34.1,
                    snowfall = 21.2
                }
            });
        }
    }
}
