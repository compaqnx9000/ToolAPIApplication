using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;
using ToolAPIApplication.Services;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class FalloutController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        private const double m2ft = 3.28084;

        public FalloutController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls> options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

        [HttpPost("fallout")]
        public IActionResult Fallout([FromBody] NbombBO bo)
        {
            if (bo.Yield < 1000 || bo.Yield>100000000)
            {
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "输入当量必须在1 - 100,000千吨之间",
                    return_data = ""
                });
            }
                
            // 需要调用天气的接口获取风速和风向
            // 接口：  POST http://192.168.10.202/commonapi/weatherservice/info

            /* 请求体 */

            //   {
            //       "lon":110.12,
            //       "lat":23.23,
            //       "impactTimeUtc":1589270745.12
            //    }

            /* 返回值 */

            //{
            //    "return_status": 0,
            //    "return_msg": "查询成功",
            //    "return_data": {
            //        "season":2,
            //        "wind_speed":12.3,
            //        "wind_dir":23.12,
            //        "temperature":23.2,
            //        "humidity":23.4,
            //        "rainfall":34.1,
            //        "snowfall":21.2
            //    }
            //}

            var str = JsonConvert.SerializeObject(bo);

            double wind_speed = 15;
            double wind_dir = 225;

            string url = _config.Weather;//https://localhost:5001/weather

            var timeUtc = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;

            WeatherBO weatherBO = new WeatherBO(bo.Lon, bo.Lat, bo.Alt, timeUtc);
            string postBody = JsonConvert.SerializeObject(weatherBO);

            try
            {
                Task<string> s = MyCore.Utils.HttpCli.PostAsyncJson(url, postBody);
                s.Wait();
                JObject jo = (JObject)JsonConvert.DeserializeObject(s.Result);//或者JObject jo = JObject.Parse(jsonText);

                wind_speed = Double.Parse(jo["return_data"]["wind_speed"].ToString());
                wind_dir = Double.Parse(jo["return_data"]["wind_dir"].ToString());
            }
            catch (Exception)
            {

            }


            var result = _geometryAnalysisService.GetFalloutGeometryJson(bo, wind_speed, wind_dir,1);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = result
            });
        }
    }
}
