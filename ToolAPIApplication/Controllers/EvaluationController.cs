using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToolAPIApplication.bo;
using ToolAPIApplication.core;
using ToolAPIApplication.dto;
using ToolAPIApplication.enums;
using ToolAPIApplication.Services;
using ToolAPIApplication.Utils;

namespace ToolAPIApplication.Controllers
{
    [ApiController]
    public class EvaluationController : ControllerBase
    {
        private readonly IGeometryAnalysisService _geometryAnalysisService;
        private ServiceUrls _config;

        private const double m2ft = 3.28084;

        public EvaluationController(IGeometryAnalysisService geometryAnalysisService,
            IOptions<ServiceUrls>  options)
        {
            _geometryAnalysisService = geometryAnalysisService ??
                throw new ArgumentNullException(nameof(geometryAnalysisService));

            _config = options.Value;
        }

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


        [HttpPost("merge")]
        public IActionResult Merge([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            if (transformBO.Yield <= 0 || transformBO.Yield > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.Yield must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            if (transformBO.nuclearExplosionID == null)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.nuclearExplosionID cannot be empty",
                    return_data = ""
                });

            double fireball_radius = _geometryAnalysisService.GetFireBallRadius(transformBO);
            double nuclearradiation_radius = _geometryAnalysisService.GetNuclearRadiationRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Light);
            double airblast_radius = _geometryAnalysisService.GetShockWaveRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Light);
            double thermalradiation_radius = _geometryAnalysisService.GetThermalRadiationRadius(transformBO.Yield, transformBO.Alt ,DamageEnumeration.Light);
            
            // 核电磁脉冲输入要求： 当量：吨，高度：km
            double nuclearpulse_radius = _geometryAnalysisService.GetNuclearPulseRadius(bo.Yield,bo.Alt/1000.0, DamageEnumeration.Light);
            // 返回也是km，所以要乘以1000.
            nuclearpulse_radius *= 1000;

            List<MergeDTO> list = new List<MergeDTO>();
            list.Add(new MergeDTO("H火球", new DamageResultDTO(bo.nuclearExplosionID, Math.Round(fireball_radius, 2), bo.Lon, bo.Lat, bo.Alt)));
            list.Add(new MergeDTO("核辐射", new DamageResultDTO(bo.nuclearExplosionID, Math.Round(nuclearradiation_radius, 2), bo.Lon, bo.Lat, bo.Alt)));
            list.Add(new MergeDTO("冲击波", new DamageResultDTO(bo.nuclearExplosionID, Math.Round(airblast_radius, 2), bo.Lon, bo.Lat, bo.Alt)));
            list.Add(new MergeDTO("热/光辐射", new DamageResultDTO(bo.nuclearExplosionID, Math.Round(thermalradiation_radius, 2), bo.Lon, bo.Lat, bo.Alt)));
            list.Add(new MergeDTO("电磁脉冲", new DamageResultDTO(bo.nuclearExplosionID, Math.Round(nuclearpulse_radius, 2), bo.Lon, bo.Lat, bo.Alt)));

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = list
            });
        }

        [HttpPost("fireball")]
        public IActionResult Fireball([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            if (transformBO.Yield <= 0 || transformBO.Yield > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            double radius = _geometryAnalysisService.GetFireBallRadius(transformBO);
           

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    bo.nuclearExplosionID,
                    DamageRadius = Math.Round(radius, 2),
                    lon = bo.Lon,
                    lat = bo.Lat,
                    alt = bo.Alt
                }
            });
        }
    

        [HttpPost("nuclearradiation")]
        public IActionResult Nuclearradiation([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            if (transformBO.Yield <= 0 || transformBO.Yield > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            double radius =  _geometryAnalysisService.GetNuclearRadiationRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Light);
            //double radius2 = _geometryAnalysisService.GetNuclearRadiationRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Heavy);
            //double radius3 = _geometryAnalysisService.GetNuclearRadiationRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Destroy);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    bo.nuclearExplosionID,
                    DamageRadius = Math.Round(radius, 2),
                    lon = bo.Lon,
                    lat = bo.Lat,
                    alt = bo.Alt
                }
            });
        }

        [HttpPost("airblast")]
        public IActionResult Airblast([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            if (transformBO.Yield <= 0 || transformBO.Yield > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            double radius = _geometryAnalysisService.GetShockWaveRadius(transformBO.Yield, bo.Alt,DamageEnumeration.Light);
            
            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    bo.nuclearExplosionID,
                    DamageRadius = Math.Round(radius, 2),
                    lon = bo.Lon,
                    lat = bo.Lat,
                    alt = bo.Alt
                }
            });
        }

        [HttpPost("thermalradiation")]
        public IActionResult Thermalradiation([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            if (transformBO.Yield <= 0 || transformBO.Yield > 100000)
                return new JsonResult(new
                {
                    return_status = 1,
                    return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
                    return_data = ""
                });

            double radius = _geometryAnalysisService.GetThermalRadiationRadius(transformBO.Yield, transformBO.Alt,DamageEnumeration.Light);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    bo.nuclearExplosionID,
                    DamageRadius = Math.Round(radius, 2),
                    lon = bo.Lon,
                    lat = bo.Lat,
                    alt = bo.Alt
                }
            });
        }

        [HttpPost("nuclearpulse")]
        public IActionResult Nuclearpulse([FromBody] NbombBO bo)//Tested
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 核电磁脉冲的当量就是“吨”，这里就不需要变成“千吨”了

            // 爆高需要传入km，所以要除以1000.
            transformBO.Alt /= 1000;


            double radius = _geometryAnalysisService.GetNuclearPulseRadius(transformBO.Yield, transformBO.Alt, DamageEnumeration.Light);

            // 返回的是km，所以要乘以1000，变成米。
            radius *= 1000;

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    bo.nuclearExplosionID,
                    DamageRadius = Math.Round(radius, 2),
                    lon = bo.Lon,
                    lat = bo.Lat,
                    alt = bo.Alt
                }
            });
        }

        [HttpPost("fallout")]
        public IActionResult Fallout([FromBody] NbombBO bo)
        {
            NbombBO transformBO = (NbombBO)bo.Clone();

            // 传入的是吨，要变成千吨
            transformBO.Yield /= 1000;

            // 输入的是米：要变成：英尺
            transformBO.Alt *= 3.2808399;

            //if (transformBO.Yield < 1000)
            //    return new JsonResult(new
            //    {
            //        return_status = 1,
            //        return_msg = "bo.equivalent must be greater than 1000 tons",
            //        return_data = ""
            //    });
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
                Task<string> s = PostAsyncJson(url, postBody);
                s.Wait();
                JObject jo = (JObject)JsonConvert.DeserializeObject(s.Result);//或者JObject jo = JObject.Parse(jsonText);

                wind_speed = Double.Parse(jo["return_data"]["wind_speed"].ToString());
                wind_dir   = Double.Parse(jo["return_data"]["wind_dir"].ToString());
            }
            catch (Exception)
            {
                
            }


            string json = _geometryAnalysisService.GetFalloutGeometryJson(transformBO, wind_speed, wind_dir);

            return new JsonResult(new
            {
                return_status = 0,
                return_msg = "",
                return_data = new
                {
                    damageGeometry = json,
                    radValue = 1,
                    nuclearExplosionID = bo.nuclearExplosionID
                }
            });
        }

        public static async Task<string> PostAsyncJson(string url, string json)
        {
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }


        //private IActionResult Check(NbombBO bo, string name)
        //{
        //    if (bo.Yield <= 0 || bo.Yield > 100000)
        //        return new JsonResult(new
        //        {
        //            return_status = 1,
        //            return_msg = "bo.equivalent must be greater than 0 and less than or equal to 100000",
        //            return_data = ""
        //        });

        //    double radius = 0;

        //    if (name.Equals("Fireball"))
        //        radius = _geometryAnalysisService.GetFireBallRadius(bo);
        //    else if (name.Equals("Nuclearradiation"))
        //        radius = _geometryAnalysisService.GetNuclearRadiationRadius(bo.Yield, DamageEnumeration.Light);
        //    else if (name.Equals("Airblast"))
        //        radius = _geometryAnalysisService.GetShockWaveRadius(bo.Yield, bo.Alt,DamageEnumeration.Light);
        //    else if (name.Equals("Thermalradiation"))
        //        radius = _geometryAnalysisService.GetThermalRadiationRadius(bo.Yield, DamageEnumeration.Light);
        //    else if (name.Equals("Nuclearpulse"))
        //        radius = _geometryAnalysisService.GetNuclearPulseRadius(bo.Yield, DamageEnumeration.Light);

        //    return new JsonResult(new
        //    {
        //        return_status = 0,
        //        return_msg = "",
        //        return_data = new
        //        {
        //            bo.nuclearExplosionID,
        //            DamageRadius = Math.Round(radius, 2),
        //            lon = bo.Lon,
        //            lat = bo.Lat,
        //            alt = bo.Alt
        //        }
        //    });
        //}

    }
}
