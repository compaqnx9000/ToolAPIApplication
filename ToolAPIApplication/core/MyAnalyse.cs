using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.enums;

namespace ToolAPIApplication.core
{
    public class Coor
    {
        public double lat;
        public double lng;

        public Coor(double _lat, double _lng)
        {
            lat = _lat;
            lng = _lng;
        }
    };
    public class MyAnalyse
    {
        const double mi2m = 1609.34;
        const double ft2mi = 0.000189394;
        const double deg2rad = 0.017453292519943295;

        const double rad2deg = 57.29577951308232;
        private int[] psi_index = new int[] { 1, 2, 4, 6, 8, 10, 15, 20, 30, 50, 100, 200, 500, 1000, 2000, 5000, 10000 };
        private Dictionary<double, int[]> hobs = new Dictionary<double, int[]>(); //heights
        private Dictionary<double, int[]> rngs = new Dictionary<double, int[]>(); //corresponding ranges
        private Dictionary<string, Dictionary<string, double[]>> eq = new Dictionary<string, Dictionary<string, double[]>>();

        // 计算冲击波带空爆的时候用
        private Dictionary<double, int[]> hobs_sm = new Dictionary<double, int[]>(); //heights
        private Dictionary<double, int[]> rngs_sm = new Dictionary<double, int[]>(); //corresponding ranges

        public MyAnalyse()
        {
            init();
        }

        /// <summary>
        /// 冲击波半径
        /// </summary>
        /// <param name="kt">当量。千顿</param>
        /// <param name="psi"></param>
        /// <returns>半径。米</returns>
        public double CalcShockWaveRadius(double kt, double ft, double psi)
        {
            double t = range_from_psi_hob(kt, psi, ft) * ft2mi;
            return t * mi2m;
        }

        /// <summary>
        /// 核辐射半径
        /// </summary>
        /// <param name="kt">当量</param>
        /// <param name="hob_ft">高度（单位feet）</param>
        /// <param name="level">辐射级别</param>
        /// <returns>半径。米</returns>
        public double CalcNuclearRadiationRadius(double kt, double hob_ft, double rem)
        {

            //bool airburst = true;//空爆为真
            bool airburst = hob_ft > 0 ? true : false;


            double r_kt = 0;
            bool erw = false; //强辐射弹头
            if (erw)
            {
                r_kt = kt * 10;
            }
            else
            {
                r_kt = kt;
            }
            var t = initial_nuclear_radiation_distance(r_kt, rem, false);

            if (hob_ft > 0 && airburst)
            {
                t = ground_range_from_slant_range(t, hob_ft * ft2mi);
            }

            return t * mi2m;
        }

        // 光（热）辐射
        public double CalcThermalRadiationRadius(double kt, double hob_ft, string threm)
        {
            // 输入
            bool airburst = hob_ft > 0 ? true : false;


            double t = thermal_distance(kt, threm, airburst);
            if (hob_ft > 0 && airburst)
            {
                t = ground_range_from_slant_range(t, hob_ft * ft2mi);
            }
            return t * mi2m;
        }

        // 光（热）辐射 通过cal /cm² 计算半径
        public double GetThermalRadiationR(double kt, double hob_ft, double cal)
        {
            double d = -1;

            // 输入
            bool airburst = hob_ft > 0 ? true : false;

            //    if (kt < 1)
            //    {
            //        //low yield scaling					
            //        var d1 = thermal_radiation_distance(thermal_radiation_param_q(1, therm), 1, airburst);
            //        d = scaled_yield(kt, d1, 1);
            //    }

            d = thermal_radiation_distance(cal, kt, airburst);//js 593 6.63932241712325

            return d * mi2m;//公里
        }

        // 核电磁脉冲
        /// <summary>
        /// 根据输入的E，计算出对应的半径R
        /// </summary>
        /// <param name="t">核爆当量（吨）</param>
        /// <param name="hob_km">高度（km）</param>
        /// <param name="e">V/m</param>
        /// <returns>km</returns>
        public double CalcNuclearPulseRadius(double t, double km, double e)
        {
            const double e0 = 0.89784 * 10000; // 单位：V/m （原来是5次方，现在改成4次方）
            double temp = e / (e0 * Math.Pow(t * 1000.0, 1 / 3.0) / Math.Pow(Math.E, Math.Pow(Math.Abs(km - 5), 1 / 2.0)));

            return Math.Sqrt(1 / temp) - 0.01;
        }
        // 计算火球半径
        public double CalcfireBallRadius(double kt, bool airburst)
        {
            //miles 转成 m
            return fireball_radius(kt, airburst) * mi2m;
        }

        //maximum fireball radius, input yield and whether airburst, output miles
        private double fireball_radius(double yield, bool airburst)
        {

            if (airburst)
                return .03788 * Math.Pow(yield, .4);
            return .04924 * Math.Pow(yield, .4);
        }

        // 放射性尘埃
        public List<Coor> CalcRadioactiveFalloutRegion(double lng,
                                                double lat,
                                                double hob_ft,
                                                double kt,
                                                double fallout_wind,
                                                double fallout_angle,
                                                DamageEnumeration level,
                                                ref double maximumDownwindDistance,
                                                ref double maximumWidth)
        {
            // 输入
            //double lng = 116.391667;
            //double lat = 39.903333;
            //int kt = 1000;
            //int fallout_wind = 15;
            //int fallout_angle = 225;
            int ff = 100;//%  fallout_fission
            //int hob_ft = 200;
            bool airburst = hob_ft > 0 ? true : false;

            if (fallout_wind < 0)
            {
                //bind the wind speed to respectable options
                fallout_wind = 0;
            }
            else if (fallout_wind > 50)
            {
                fallout_wind = 50;
            }


            if (hob_ft > 0)
            {
                // 空爆
                return do_fallout(lng, lat, kt, fallout_wind, ff, fallout_angle, airburst, hob_ft, level,
                    ref maximumDownwindDistance, ref maximumWidth);
            }
            else
            {
                return do_fallout(lng, lat, kt, fallout_wind, ff, fallout_angle, airburst, -1, level,
                    ref maximumDownwindDistance, ref maximumWidth);
            }
        }



        private List<Coor> do_fallout(double lng, double lat,
            double kt, double wind, double fission_fraction, double angle, bool airburst, double hob_ft, DamageEnumeration level,
            ref double maximumDownwindDistance, ref double maximumWidth)
        {
            if (kt < 1 || kt > 10 * Math.Pow(10, 5))
            {
                return null;
            }

            //double lng = 116.391667;
            //double lat = 39.903333;



            //new_windsock(angle);
            //if (!angle) angle = fallout_bearing(marker.getLatLng(), windsock_marker.getLatLng());

            double kt_frac = 0;

            if (hob_ft > 0)
            {
                kt_frac = fallout_kt_hob(kt, fission_fraction, hob_ft);
            }
            else
            {
                kt_frac = 0;
            }

            // fallout_current = {
            //      kt: kt,                                         1000       
            //      wind: wind,  15
            //      fission_fraction: fission_fraction,             100
            //      fallout_info_div_id:                            fallout_info_div_id,
            //      rad_doses: rad_doses,                           [1,10,100,1000]
            //      angle: angle,                                   225
            //      airburst: airburst,                             false
            //      hob_ft: hob_ft,                                 null
            //      kt_frac:                                        0
            //}
            int[] rad_doses = new int[] { 1, 10, 100, 1000 };

            return draw_fallout(lng, lat, angle, kt, kt_frac, rad_doses, airburst, fission_fraction, wind, level, ref maximumDownwindDistance, ref maximumWidth);

        }

        //draws fallout using the current settings. we keep these separate from the other function so I can just call it whenever I want to refresh it.
        private List<Coor> draw_fallout(double lng, double lat, double angle,
            double kt, double kt_frac, int[] rad_doses,
            bool airburst, double fission_fraction, double wind, DamageEnumeration level,
            ref double maximumDownwindDistance, ref double maximumWidth)
        {
            Dictionary<int, Dictionary<string, double>> sfss;
            if (kt_frac > 0 && airburst)
            {
                sfss = SFSS_fallout(kt_frac, rad_doses, (fission_fraction / 100), wind);
            }
            else
            {
                sfss = SFSS_fallout(kt, rad_doses, (fission_fraction / 100), wind);
            }



            var steps = 15;

            // 下面2行不知道有啥用
            //dets[det_index].fallout_angle = Math.round(fallout_current.angle);//225
            //dets[det_index].fallout_rad_doses = fallout_current.rad_doses;//[1,10,100,1000]

            //double lng = 116.391667;
            //double lat = 39.903333;
            //int angle = 225;
            //for (var i = 3; i < 4; i++)
            //{
            //    //var ss = sfss[1];//i = 0
            //    //var ss = sfss[10];//i = 1
            //    //var ss = sfss[100];//i = 2
            //    var ss = sfss[1000];//i = 3

            //    //sfss[1do_fallout0],sfss[100],sfss[1000]
            //    List<Coor> ll = plot_fallout(lng, lat, SFSS_fallout_points(ss, angle, steps), rad_doses[i]);
            //    foreach (Coor c in ll)
            //    {
            //        Console.WriteLine(c.lng.ToString() + "," + c.lat.ToString() + ",");

            //    }

            //}
            var ss = sfss[1];//i = 3
            switch (level)
            {
                case DamageEnumeration.Light:
                    ss = sfss[1];
                    break;
                case DamageEnumeration.Heavy:
                    ss = sfss[100];
                    break;
                case DamageEnumeration.Destroy:
                    ss = sfss[1000];
                    break;
            }
            List<Coor> ll = plot_fallout(lng, lat, SFSS_fallout_points(ss, angle, steps), (int)level);
            const double mi2km = 1.60934;
            maximumDownwindDistance = ss["downwind_cloud_distance"] * mi2km;
            maximumWidth = ss["max_cloud_width"] * 2 * mi2km;
            return ll;
        }

        private List<Coor> plot_fallout(double lng, double lat, List<Coor> points, double rad)
        {
            int R = 6371; //Earth's mean radius in km
            List<Coor> coords = new List<Coor>();
            for (int i = 0; i < points.Count; i++)
            {
                const double mi2km = 1.60934;
                double gzlat = lat + rad2deg * (points[i].lat * mi2km / R);
                double gzlng = lng + rad2deg * (points[i].lng * mi2km / R / Math.Cos(deg2rad * (lat)));
                coords.Add(new Coor(gzlat, gzlng));
            }

            return coords;
        }

        //f is a single dose_data object returned by the above function
        private List<Coor> SFSS_fallout_points(Dictionary<string, double> f, double angle, int steps)
        {
            List<Coor> p = new List<Coor>();


            var stem_circle_radius = (f["x_center"] - f["upwind_stem_distance"]);
            var stem_inner_x = Math.Sin(80 * deg2rad) * stem_circle_radius;

            if (f["draw_stem"] == 1)
            {
                //stem top		
                draw_arc(ref p, f["upwind_stem_distance"], 0, f["x_center"], stem_circle_radius, steps / 2.0);
                draw_arc(ref p, f["x_center"], stem_circle_radius, stem_inner_x, f["max_stem_width"], steps / 2.0);

                //test if the stem and the cloud join
                if (((f["upwind_cloud_distance"] + f["x_center"]) < f["downwind_stem_distance"]) && f["draw_cloud"] == 1)
                {
                    List<Coor> pp = new List<Coor>();
                    draw_arc(ref pp, f["upwind_cloud_distance"] + f["x_center"], 0, f["cloud_widen_point"] + f["x_center"], f["max_cloud_width"], steps);
                    pp = trim_points(ref pp, 1, f["max_stem_width"], "<");//这个其实不用返回，直接就修改了输入的pp了
                    add_points(ref p, ref pp);
                }
                else
                {

                    p.Add(new Coor(f["downwind_stem_distance"] * .8, f["max_stem_width"]));
                    p.Add(new Coor(f["downwind_stem_distance"], 0));

                    if (f["draw_cloud"] == 1)
                        draw_arc(ref p, f["upwind_cloud_distance"] + f["x_center"], 0, f["cloud_widen_point"] + f["x_center"], f["max_cloud_width"], steps);
                }
            }
            else
            {
                if (f["draw_cloud"] == 1)
                    draw_arc(ref p, f["upwind_cloud_distance"] + f["x_center"], 0, f["cloud_widen_point"] + f["x_center"], f["max_cloud_width"], steps);
            }

            if (f["draw_cloud"] == 1)
            {
                //cloud
                draw_arc(ref p, f["cloud_widen_point"] + f["x_center"], f["max_cloud_width"], f["downwind_cloud_distance"] - f["x_center"] * 2, 0, steps);
                draw_arc(ref p, f["downwind_cloud_distance"] - f["x_center"] * 2, 0, f["cloud_widen_point"] + f["x_center"], -f["max_cloud_width"], steps);
            }

            if (f["draw_stem"] == 1)
            {
                //stem bottom
                if (((f["upwind_cloud_distance"] + f["x_center"]) < f["downwind_stem_distance"]) && f["draw_cloud"] == 1)
                {
                    List<Coor> pp = new List<Coor>();
                    draw_arc(ref pp, f["cloud_widen_point"] + f["x_center"], -f["max_cloud_width"], f["upwind_cloud_distance"] + f["x_center"], 0, steps);
                    pp = trim_points(ref pp, 1, -f["max_stem_width"], ">");
                    add_points(ref p, ref pp);
                }
                else
                {
                    if (f["draw_cloud"] == 1)
                        draw_arc(ref p, f["cloud_widen_point"] + f["x_center"], -f["max_cloud_width"], f["upwind_cloud_distance"] + f["x_center"], 0, steps);

                    p.Add(new Coor(f["downwind_stem_distance"], 0));
                    p.Add(new Coor(f["downwind_stem_distance"] * .8, -f["max_stem_width"]));

                }
                draw_arc(ref p, stem_inner_x, -f["max_stem_width"], f["x_center"], -stem_circle_radius, steps / 2.0);
                draw_arc(ref p, f["x_center"], -stem_circle_radius, f["upwind_stem_distance"], 0, steps / 2.0);
            }
            else
            {
                if (f["draw_cloud"] == 1)
                    draw_arc(ref p, f["cloud_widen_point"] + f["x_center"], -f["max_cloud_width"], f["upwind_cloud_distance"] + f["x_center"], 0, steps);
            }

            rotate_points(ref p, angle);

            return p;

        }

        //trims points based on criteria
        //input is an array of points (p), a lat/lng flag (0 = lat, 1 = lng), a value to compare to (compare), and a comparison mode (string)
        private List<Coor> trim_points(ref List<Coor> p, double latlng, double compare, string mode)
        {
            List<Coor> pp = new List<Coor>();

            for (var i = 0; i < p.Count; i++)
            {
                bool bad = false;
                switch (mode)
                {
                    case "<": bad = p[i].lng < compare; break;
                    case "<=": bad = p[i].lng <= compare; break;
                    case ">": bad = p[i].lng > compare; break;
                    case ">=": bad = p[i].lng >= compare; break;
                    case "==": bad = p[i].lng == compare; break;
                    case "!=": bad = p[i].lng != compare; break;
                }
                if (!bad)
                    pp.Add(p[i]);//p[i]是数组
            }
            return pp;
        }

        //adds points arrays from pp to p
        private void add_points(ref List<Coor> p, ref List<Coor> pp)
        {
            for (var i = 0; i < pp.Count; i++)
            {
                p.Add(pp[i]);
            }
        }

        private void rotate_points(ref List<Coor> points_array, double angle_degrees)
        {
            //normalize angle for wind
            angle_degrees = (90 - angle_degrees) + 180;
            //rotate
            double angle_rad = (angle_degrees) * deg2rad;
            double sinA = Math.Sin(angle_rad);
            double cosA = Math.Cos(angle_rad);
            for (int i = 0; i < points_array.Count; i++)
            {
                double px = points_array[i].lat;
                double py = points_array[i].lng;
                points_array[i].lat = px * cosA - py * sinA;
                points_array[i].lng = px * sinA + py * cosA;
            }
        }

        //draws a partial arc joining points x1,y1 and x2,y2 centered at xc,yc
        private void draw_arc(ref List<Coor> p, double x1, double y1, double x2, double y2, double steps)
        {
            double xc, yc;
            if (x1 < x2)
            {
                if (y1 < y2)
                {
                    //top left
                    xc = x2;
                    yc = y1;
                }
                else
                {
                    //top right
                    xc = x1;
                    yc = y2;
                }
            }
            else
            {
                if (y1 < y2)
                {
                    //bottom left
                    xc = x1;
                    yc = y2;
                }
                else
                {
                    //bottom right				
                    xc = x2;
                    yc = y1;
                }
            }

            double e_width = Math.Abs(xc == x1 ? xc - x2 : xc - x1);
            double e_height = Math.Abs(yc == y1 ? yc - y2 : yc - y1);

            double start_angle = Math.Atan2(y1 - yc, x1 - xc);
            double stop_angle = Math.Atan2(y2 - yc, x2 - xc);

            if (start_angle < 0) start_angle += Math.PI * 2;

            double step = (stop_angle - start_angle) / steps;

            if (step < 0)
            {
                for (double theta = start_angle; theta > stop_angle; theta += step)
                {
                    double x = xc + e_width * Math.Cos(theta);
                    double y = yc + e_height * Math.Sin(theta);
                    p.Add(new Coor(x, y));

                }
            }
            else
            {
                for (double theta = start_angle; theta < stop_angle; theta += step)
                {
                    double x = xc + e_width * Math.Cos(theta);
                    double y = yc + e_height * Math.Sin(theta);
                    p.Add(new Coor(x, y));
                }
            }

            p.Add(new Coor(x2, y2));
        }


        //returns in miles -- one might wonder why we separate this from the points function. It is so you can access this data (say, for the legend) without complete recalculation of all of the information.

        //rad_doses is an array of radiation doses in rads/hr to computer

        //fission fraction is a number less than or equal to 1 (100%) and greater than zero
        private Dictionary<int, Dictionary<string, double>> SFSS_fallout(double kt, int[] rad_doses, double fission_fraction, double windspeed)
        {
            //kt = 1000; fission_fraction = 1 (100%),windspeed = 15,rad_doses=[1,10,100,1000]
            //var x_var = 0;
            //var y_var = 1;
            //var i_var = 2;
            Dictionary<string, double> p = SFSS_fallout_params(kt);//p的计算结果是对的

            fission_fraction = 1;// 这里固定p就=1

            Dictionary<int, Dictionary<string, double>> dose_data = new Dictionary<int, Dictionary<string, double>>();

            for (var i = 0; i < rad_doses.Length; i++)
            {
                double rad = rad_doses[i];

                //create the dose_data object -- all input distances in feet, all output in miles

                Dictionary<string, double> d = new Dictionary<string, double>();//d的结果是对的

                d["r"] = rad_doses[i];
                d["draw_stem"] = (rad <= p["i2"]) ? 1 : 0;
                d["draw_cloud"] = (rad <= p["i7"]) ? 1 : 0;
                d["max_cloud_rad"] = p["i7"] / (1 / fission_fraction);
                d["max_stem_rad"] = p["i2"] / (1 / fission_fraction); //ditto
                d["x_center"] = p["x2"] * ft2mi;
                d["upwind_stem_distance"] = log_lerp(p["x1"], p["i1"], p["x2"], p["i2"], rad) * ft2mi;
                d["downwind_stem_distance"] = log_lerp(p["x3"], p["i3"], p["x4"], p["i4"], rad) * ft2mi;
                d["max_stem_width"] = log_lerp(0, p["i2"], p["ys"], 1, rad) * ft2mi;
                d["upwind_cloud_distance"] = (rad > p["i6"] ? log_lerp(p["x6"], p["i6"], p["x7"], p["i7"], rad) : log_lerp(p["x5"], p["i5"], p["x6"], p["i6"], rad)) * ft2mi;
                d["downwind_cloud_distance"] = log_lerp(p["x7"], p["i7"], p["x9"], p["i9"], rad) * ft2mi;
                d["max_cloud_width"] = (p["y8"] * log10(p["i7"] / rad)) / (log10(p["i7"] / p["i9"])) * ft2mi;
                d["cloud_widen_point"] = (
                        p["x7"] + (p["x8"] - p["x7"]) * (
                        (p["y8"] * log10(p["i7"] / rad)) / (log10(p["i7"] / p["i9"])) / p["y8"])
                    ) * ft2mi;

                //adjust for wind speed, uses Glasstone 1977's relation to change downwind values
                double wm = 0;
                // 传入的windspee=15，所以if和else都没执行
                if (windspeed > 15)
                {
                    wm = (1 + ((windspeed - 15) / 60));
                }
                else if (windspeed < 15)
                {
                    wm = (1 + ((windspeed - 15) / 30));
                }
                if (wm != 0)
                {
                    d["downwind_cloud_distance"] *= wm;
                    d["downwind_stem_distance"] *= wm;
                }

                double stem_area = 0;
                //estimate of the area enclosed -- sq mi
                if (d["draw_stem"] == 1)
                {
                    stem_area = (
                        //stem - ellipse estimate -- this is OK
                        (
                        Math.PI * ((d["downwind_stem_distance"] - d["upwind_stem_distance"]) / 2) * (d["x_center"] - d["upwind_stem_distance"])
                        )
                        );

                    d["stem_area"] = stem_area;

                }
                else
                {
                    stem_area = 0;
                    d["stem_area"] = stem_area;
                }

                double cloud_area = 0;
                if (d["draw_cloud"] == 1)
                {
                    cloud_area = (
                        //cloud ellipse 1			
                        (
                        Math.PI * (d["downwind_cloud_distance"]) * (d["max_cloud_width"])
                        ) / 2

                        //cloud ellipse 2
                        +
                        (
                        Math.PI * d["downwind_cloud_distance"] - ((d["cloud_widen_point"]) / 2 - (d["upwind_cloud_distance"]) / 2) * (d["max_cloud_width"])
                        ) / 2
                    );
                    d["cloud_area"] = cloud_area;
                }
                else
                {
                    cloud_area = 0;
                    d["cloud_area"] = cloud_area;
                }

                dose_data[(int)rad_doses[i]] = d;
            }



            return dose_data;//dose_data应该是一个字典
        }

        //linear interpolation that gets a log of all Y values first
        private double log_lerp(double x1, double y1, double x2, double y2, double y3)
        {
            return lerp(x1, Math.Log(y1), x2, Math.Log(y2), Math.Log(y3));
        }



        //Fallout parameters derived from Miller's Simplified Fallout Scaling System 
        private Dictionary<string, double> SFSS_fallout_params(double kt)
        {

            if (kt < 1 || kt > 10 * Math.Pow(10, 5))
            {
                return null;
            }

            var logW = log10(kt); //to avoid recalculation

            //alpha values (p.249-250)
            var alpha_2_3 = unlog10(-0.509 + 0.076 * logW);
            var alpha_4 = unlog10(0.270 + 0.089 * logW);
            var alpha_5 = unlog10(-0.176 + 0.022 * logW);
            var alpha_5pr = unlog10(-0.054 + 0.095 * logW);
            var alpha_6 = unlog10(0.030 + 0.036 * logW);
            var alpha_7 = unlog10(0.043 + 0.141 * logW);
            var alpha_8 = unlog10(0.185 + 0.151 * logW);
            double alpha_9 = 0;
            if (kt <= 28)
            {
                alpha_9 = unlog10(1.371 - 0.124 * logW);
            }
            else
            {
                alpha_9 = unlog10(0.980 + 0.146 * logW);
            }

            //pre_reqs for X-distances (p.250)
            var a_s = unlog10(2.880 + 0.348 * logW);
            var a = unlog10(3.389 + 0.431 * logW);
            var b = 1.40 * Math.Pow(10, 3) * Math.Pow(kt, 0.431);
            double h = 0;
            if (kt <= 28)
            {
                h = unlog10(3.820 + 0.445 * logW);
            }
            else
            {
                h = unlog10(4.226 + 0.164 * logW);
            }
            var a_R_s = unlog10(1.070 + 0.098 * logW);
            var R_s = unlog10(2.319 + 0.333 * logW);
            var a_o = unlog10(log10(a) - (h * log10(a_R_s)) / (h - R_s));

            var k_a = 2.303 * (log10(a_R_s) / (h - R_s));
            var z_s = (2.303 * (log10(a_s) - log10(a_o))) / k_a; //typo in the original!!
            double z_o = 0;
            if (kt >= 9)
            {
                z_o = (1900 + (alpha_2_3 + 0.020) * z_s) / alpha_2_3;
            }
            else
            {
                z_o = (h - b);

            }

            double X_1, X_2, X_3, X_4, X_5, X_6, X_7, X_8, X_9;

            //X-distances (p.251)
            if (kt <= 28)
            {
                X_1 = -unlog10(3.308 + 0.496 * logW);
                X_5 = unlog10(3.644 + 0.467 * logW);
                X_6 = unlog10(3.850 + 0.481 * logW);
                X_7 = unlog10(3.862 + 0.586 * logW);
                X_8 = unlog10(4.005 + 0.596 * logW);
                X_9 = unlog10(5.190 + 0.319 * logW);
            }
            else
            {
                X_1 = -unlog10(3.564 + 0.319 * logW);
                X_5 = unlog10(4.049 + 0.186 * logW);
                X_6 = unlog10(4.255 + 0.200 * logW);
                X_7 = unlog10(4.268 + 0.305 * logW);
                X_8 = unlog10(4.410 + 0.315 * logW);
                X_9 = unlog10(5.202 + 0.311 * logW);
            }
            var Y_s = unlog10(3.233 + 0.400 * logW);

            //p. 250
            X_2 = alpha_2_3 * z_s - a_s;
            X_3 = alpha_2_3 * z_s + a_s;
            X_4 = (alpha_4 * (alpha_4 * z_o - 1900)) / (alpha_4 + 0.020);
            double k_1_2 = 0;

            //intensity ridges (p.251)
            if (kt <= 28)
            {
                k_1_2 = unlog10(-2.503 - 0.404 * logW);
            }
            else
            {
                k_1_2 = unlog10(-2.600 - 0.337 * logW);
            }
            var I_2_3 = unlog10(k_1_2 * (X_2 - X_1) / 2.303);
            double a_h = 0;

            if (kt <= 28)
            {
                a_h = unlog10(-0.431 - 0.014 * logW);
            }
            else
            {
                a_h = unlog10(-0.837 + 0.267 * logW);
            }
            var a_b_2 = unlog10(0.486 + 0.262 * logW);

            var phi_5 = ((alpha_5 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_5 + a_h, 2))) / ((alpha_5 - a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_5 - a_h, 2)));
            var phi_6 = ((alpha_6 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_6 + a_h, 2))) / ((alpha_6 - a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_6 - a_h, 2)));
            var phi_7 = ((alpha_7 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_7 + a_h, 2))) / ((alpha_7 - a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_7 - a_h, 2)));
            var phi_8 = ((alpha_8 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_8 + a_h, 2))) / ((alpha_8 - a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_8 - a_h, 2)));
            var phi_9 = ((alpha_9 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_9 + a_h, 2))) / ((alpha_9 - a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_9 - a_h, 2)));

            var phi_5pr = ((alpha_5 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_5 + a_h, 2))) / (alpha_2_3 + Math.Sqrt(a_b_2 + Math.Pow(alpha_2_3, 2)));
            var phi_6pr = ((alpha_6 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_6 + a_h, 2))) / (alpha_2_3 + Math.Sqrt(a_b_2 + Math.Pow(alpha_2_3, 2)));
            var phi_7pr = ((alpha_7 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_7 + a_h, 2))) / (alpha_2_3 + Math.Sqrt(a_b_2 + Math.Pow(alpha_2_3, 2)));
            var phi_8pr = ((alpha_8 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_8 + a_h, 2))) / (alpha_2_3 + Math.Sqrt(a_b_2 + Math.Pow(alpha_2_3, 2)));
            var phi_9pr = ((alpha_9 + a_h) + Math.Sqrt(a_b_2 + Math.Pow(alpha_9 + a_h, 2))) / (alpha_2_3 + Math.Sqrt(a_b_2 + Math.Pow(alpha_2_3, 2)));
            double K_5_A_alpha = 0;
            if (kt <= 28)
            {
                K_5_A_alpha = unlog10(-3.286 - 0.298 * logW);
            }
            else
            {
                K_5_A_alpha = unlog10(-2.889 - 0.572 * logW);
            }
            var K_6_A_alpha = unlog10(-1.134 - 0.074 * logW);
            var K_7_A_alpha = unlog10(-0.989 - 0.037 * logW);
            var K_9_A_alpha = unlog10(-2.166 - 0.552 * logW);

            var K_5pr_A_alpha = unlog10(-3.185 - 0.406 * logW);
            var K_6pr_A_alpha = unlog10(-1.225 - 0.022 * logW);
            var K_7pr_A_alpha = unlog10(-1.079 - 0.020 * logW);
            var K_9pr_A_alpha = unlog10(-2.166 - 0.552 * logW);

            var I_1 = 1; //set at 1 r/hr
            var I_4 = 1; //set at 1 r/hr
            double I_5, I_6, I_7, I_9;
            if (alpha_5 >= a_h)
            {
                I_5 = 4.606 * a * K_5_A_alpha * log10(phi_5);
            }
            else
            {
                I_5 = 4.606 * a * K_5pr_A_alpha * log10(phi_5pr);
            }
            if (alpha_6 >= a_h)
            {
                I_6 = 4.606 * a * K_6_A_alpha * log10(phi_6);
            }
            else
            {
                I_6 = 4.606 * a * K_6pr_A_alpha * log10(phi_6pr);
            }
            if (alpha_7 >= a_h)
            {
                I_7 = 4.606 * a * K_7_A_alpha * log10(phi_7);
            }
            else
            {
                I_7 = 4.606 * a * K_7pr_A_alpha * log10(phi_7pr);
            }
            // there is no I_8
            if (alpha_9 >= a_h)
            {
                I_9 = 4.606 * a * K_9_A_alpha * log10(phi_9);
            }
            else
            {
                I_9 = 4.606 * a * K_9pr_A_alpha * log10(phi_9pr);
            }

            //Y_8 is from a table with interpolation for other values
            //Each index here is a log10 of a yield (0 = 1KT, 1 = 10KT, 2 = 100KT, etc.)
            int[] Y_8_vals = new int[] { 6620, 12200, 48200, 167000, 342000, 650000 };
            double Y_8 = 0;
            if (logW == Math.Round(logW) || kt == 1000)
            { //the log10 function botches 
                Y_8 = Y_8_vals[(int)Math.Round(logW)];
            }
            else
            {
                var Y_8_1 = Y_8_vals[(int)Math.Floor(logW)];
                var Y_8_2 = Y_8_vals[(int)Math.Ceiling(logW)];
                Y_8 = Y_8_1 + (Y_8_2 - Y_8_1) * (unlog10(logW) / unlog10(Math.Ceiling(logW)));
            }
            //alternative method that just curve fits
            //var Y_8 = Math.exp(((((9.968481E-4)*Math.log(kt)-.027025999)*Math.log(kt)+.22433052)*Math.log(kt)-.12350012)*Math.log(kt)+8.7992249);

            return new Dictionary<string, double>(){
                { "x1", X_1 },
                {"x2", X_2},
                {"x3", X_3},
                {"x4", X_4},
                {"x5", X_5},
                {"x6", X_6},
                {"x7", X_7},
                {"x8", X_8},
                {"x9", X_9},
                {"ys", Y_s},
                {"y8", Y_8},
                {"i1", I_1 },
                {"i2", I_2_3 },
                {"i3", I_2_3 },
                {"i4", I_4 },
                {"i5", I_5 },
                {"i6", I_6 },
                {"i7", I_7 },
                {"i9", I_9 },
                { "zo:" ,z_o }

            };
        }

        private double log10(double n)
        {
            const double Math_LN10 = 2.302585092994046;
            return (Math.Log(n)) / (Math_LN10);
        }

        private double unlog10(double n)
        {
            return Math.Pow(10, n);
        }

        private void new_windsock(int angle)
        {
            //double pos_lng = 116.391667;
            //double pos_lat = 39.903333;

            //double m_lng = 116.391667;
            //double m_lat = 39.903333;

            ////map.getZoom() = 12
            //int zoom = 12;
            //int windsock_distance = (16 - zoom) * 2 - 1;//=7

            // 这个算出来的和nuke不一样，不知道有么有用？？？
            //double[] wpos = destination_from_bearing(m_lat, m_lng, angle + 180, windsock_distance);
            //wpos[0] = 39.84038048758569;
            //wpos[1] = 116.39166700000001;



        }

        //destination lat/lon from a start point lat/lon of a giving bearing and distance
        private double[] destination_from_bearing(double lat, double lon, int bearing, double distance)
        {

            var R = 6371; // km
            var d = distance;
            var lat1 = deg2rad * lat;
            var lon1 = deg2rad * lon;
            var brng = deg2rad * bearing;

            var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(d / R) +
                                Math.Cos(lat1) * Math.Sin(d / R) * Math.Cos(brng));
            var lon2 = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(d / R) * Math.Cos(lat1),
                                     Math.Cos(d / R) - Math.Sin(lat1) * Math.Sin(lat2));
            return new double[] { rad2deg * lat2, rad2deg * lon2 };
        }

        //this scales the fission yield according to altitude of burst. Input is kt, fission_fraction (0-1), altitude of burst (feet)
        //returns a new kilotonnage, or 0 if the hob is too high for local fallout
        //taken from eq. 4.4.1 of H.G. Norment, "DELFIC: Department of Defense Fallout Prediction System, Vol. I-Fundamentals" (DNA 5159F-1, 31 December 1979), page 53. 
        private double fallout_kt_hob(double kt, double fission_fraction, double hob)
        {
            if (hob == 0) return kt; //surface burst, no doubt
            var fission_kt = kt * (fission_fraction / 100);
            var scaled_hob_activity_decay_constant = hob / Math.Pow(kt, (1 / 3.0));
            var scaled_hob = hob / Math.Pow(kt, (1 / 3.4));
            var max_hob = 180 * Math.Pow(kt, .4); //Glasstone and Dolan, 1977 edn., p.71
            if (hob >= max_hob)
            { //using Glasstone' def of negligible fallout rather than DELFICs, because DELFICs seems about 40-50% lower for no reason
                return 0;
            }
            else
            if (scaled_hob_activity_decay_constant <= 0)
            {
                return 0;
            }
            else
            {
                var f_d = Math.Pow(0.45345, scaled_hob_activity_decay_constant / 65.0);
                var scaled_kt = (fission_kt * f_d) / (fission_fraction / 100.0);
                return scaled_kt;
            }
        }


        //wrapper for all fo the thermal radiation functions, including automatic scaling for yields >20Mt or <1Kt
        //input is kt, thermal radiation (cal/cm^2), and airburst flag
        //note can also take the thermal_radiation_params as well if preceded by underscores
        private double thermal_distance(double kt, string therm, bool airburst)
        {
            double d = -1;
            switch (therm)
            {
                case "_3rd-50":
                case "_3rd-100":
                case "_2nd-50":
                case "_1st-50":
                case "_noharm-100":
                    if (kt < 1)
                    {
                        //low yield scaling					
                        var d1 = thermal_radiation_distance(thermal_radiation_param_q(1, therm), 1, airburst);
                        d = scaled_yield(kt, d1, 1);
                    }
                    else if (kt > 20000)
                    {
                        //high yield scaling					
                        var d1 = thermal_radiation_distance(thermal_radiation_param_q(kt, therm), 20000, airburst);
                        d = scaled_yield(kt, d1, 20000);
                    }
                    else
                    {
                        //default range
                        d = thermal_radiation_distance(thermal_radiation_param_q(kt, therm), kt, airburst);
                    }
                    break;
                default:
                    d = thermal_radiation_distance(Double.Parse(therm), kt, airburst);
                    break;
            }
            return d;
        }

        //scales from one yield effect to another according to the cube root law
        private double scaled_yield(double yield, double ref_distance, double ref_yield)
        {
            return ref_distance / (Math.Pow(ref_yield / yield, 1 / 3));
        }

        //input is thermal radiation (cal/cm^2), yield, airburst flag; output is distance or slant distance (mi)
        private double thermal_radiation_distance(double radiation, double yield, bool airburst)
        {
            if (airburst)
                return eq_result("2-108", radiation * (1 / yield), -1, false);  //airburst
            return eq_result("2-108", radiation * (1 / (.7 * yield)), -1, false); //surface

            //switch (airburst)
            //{
            //    case (true):
            //        return eq_result("2-108", radiation * (1 / yield), -1, false);  //airburst
            //    case (false):
            //        return eq_result("2-108", radiation * (1 / (.7 * yield)), -1, false); //surface
            //}
        }

        /* input is yield and one of the following strings:
		_1st-50: 50% chance of 1st degree burn
		_2nd-50: 50% chance of 2nd degree burn
		_3rd-50: 50% chance of 3rd degree burn
		_3rd-100: 100% chance of 3rd degree burn
		noharm-100: 100% chance of no thermal damage (min radius)
	    output is in q (cal/cm^2), based on Glasstone and Dolan 1977 	
	    */
        private double thermal_radiation_param_q(double yield, string param)
        {
            switch (param)
            {
                case "_1st-50":
                    return Math.Log(eq_result("77-12.65-1st-50", yield, Math.E, true));
                case "_2nd-50":
                    return Math.Log(eq_result("77-12.65-2nd-50", yield, Math.E, true));
                case "_3rd-50":
                    return Math.Log(eq_result("77-12.65-3rd-50", yield, Math.E, true));
                case "_3rd-100":
                    return Math.Log(eq_result("77-12.65-3rd-100", yield, Math.E, true));
                case "_noharm-100":
                    return Math.Log(eq_result("77-12.65-noharm-100", yield, Math.E, true));
                default: return -1;
            }

        }

        //retrieves the equation specified
        private double eq_result(string eq_id, double x, double logbase, bool ignore_range)
        {
            if (eq.ContainsKey(eq_id))
            {
                if (x == -1)
                    return -1;
                else if ((x < eq[eq_id]["xmin"][0] || x > eq[eq_id]["xmax"][0]) && (ignore_range != true))
                    return -1;
                else
                    return loggo(x, eq[eq_id]["args"], logbase);
            }
            return -1;
        }

        //runs the polynomial
        private double loggo(double x, double[] args, double? logbase)
        {
            double l = 0;
            if (logbase == -1) logbase = 10;
            double logbx = logb(x, logbase.Value);
            for (var i = 0; i < args.Length; i++)
            {
                l += args[i] * Math.Pow(logbx, i);
            }
            return Math.Pow(logbase.Value, l);
        }

        //replacement logarithm function, arbitrary base
        private double logb(double n, double lbase)
        {
            if (lbase == Math.E)
            {
                return Math.Log(n);
            }
            else if (lbase == 10)
            {
                const double LN10 = 2.302585092994046;
                return (Math.Log(n)) / (LN10);
            }
            else if (lbase == 2)
            {
                const double LN2 = 0.6931471805599453;
                return (Math.Log(n)) / (LN2);
            }
            else
            {
                return (Math.Log(n)) / (Math.Log(lbase));
            }
        }

        //simple function that returns ground range from slant range and a altitude -- note there is a need for the units to be the same for both
        //this ONLY works for effects that are straightforwardly spherical in nature (thermal, radiation, but NOT pressure, because pressure reflects off of the ground)
        private double ground_range_from_slant_range(double slant_range, double altitude)
        {
            if (slant_range < altitude)
            {
                return 0;
            }
            else
            {
                return Math.Sqrt(Math.Pow(slant_range, 2) - Math.Pow(altitude, 2));
            }
        }

        //initial nuclear radiation (distance)
        //input is yield and rem; output is slant range
        //only officially valid range is yield is between 1 and 20 MT, but I have removed the checks on this since the extrapolations don't look completely insane and are better than nothing
        private double initial_nuclear_radiation_distance(double yield, double rem, bool airburst)
        {
            //if(yield>=1&&yield<=20000) {
            if (rem >= 1 && rem <= Math.Pow(10, 8))
            {

                var a = +0.1237561; var a_ = +0.0143624;
                var b = +0.0994027; var b_ = -0.0000816;
                var c = +0.0011878; var c_ = -0.0000014;
                var d = -0.0002481; var d_ = +0.0054734;
                var e = +0.0000096; var e_ = -0.0003272;
                var f = -0.1308215; var f_ = +0.0000106;
                var g = +0.0009881; var g_ = -0.0001220;
                var h = -0.0032363; var h_ = +0.0000217; //note! h is positive in the original, but this gives nonsense answers
                var i = +0.0000111; var i_ = -0.0000006; //I suspect it is a typo. what annoyance it caused me!

                var logI = Math.Log10(rem);
                var logI2 = Math.Pow(logI, 2);
                var logI3 = Math.Pow(logI, 3);
                var logW = Math.Log10(yield);

                //eq. 2.116	
                var distance = a + (b + a_ * logI + d_ * logI2 + g_ * logI3) * logW;
                distance += (c + b_ * logI + e_ * logI2 + h_ * logI3) * Math.Pow(logW, 3);
                distance += (d + (c_ * logI) + (f_ * logI2) + (i_ * logI3)) * Math.Pow(logW, 5);
                distance += (e * Math.Pow(logW, 7)) + (f * logI) + (g * logI2) + (h * logI3);
                distance += (i * Math.Pow(logI, 5));

                return Math.Pow(10, distance);
            }
            else
            {
                //this.error = "REM OUTSIDE RANGE [rem: " + rem + ", min: " + 1 + ", max: " + Math.pow(10, 8) + "]";
                //if (debug) console.log(this.error);
                return -1;
            }

        }



        //public void Test()
        //{
        //    double t  = range_from_psi_hob(1000, 3000, 0) * ft2mi;//0.20580814666666664

        //    int kt = 1000;
        //    var cr = crater(kt, true);

        //    //0: 0.23979999999999996
        //    //1: 0.11989999999999998
        //    //2: 0.05738999999999999


        //    double radius = t * mi2m; //331.21528275653327


        //    radius += radius;
        //}

        //crater functions -- input yield and flag for soil (true) or rock (false)
        //output is an array of lip radius (mi), apparent radius (mi), and depth (mi)
        private double[] crater(double yield, bool soil)
        {
            //if (yield == undefined)
            //{
            //    this.error = "MISSING INPUT PARAMETER"; if (debug) console.log(this.error); return undefined;
            //}
            double[] c = new double[3];
            if (soil)
            { //soil
                c[0] = .02398 * Math.Pow(yield, 1 / 3); //lip
                c[1] = .01199 * Math.Pow(yield, 1 / 3); //apparent
                c[2] = .005739 * Math.Pow(yield, 1 / 3); //depth
            }
            else
            { //rock
                c[0] = .01918 * Math.Pow(yield, 1 / 3); //lip
                c[1] = .009591 * Math.Pow(yield, 1 / 3); //apparent
                c[2] = .004591 * Math.Pow(yield, 1 / 3); //depth
            }
            return c;
        }

        private void init()
        {
            //these heights-of-bursts and ranges for various psi are pixel matches to the knee curve graphs in Glasstone and Dolan 1977

            var hobs_10000 = new int[] { 0, 10, 22, 33, 44, 54, 65, 73, 79, 88, 94, 99, 104, 108, 111, 114, 116, 117, 117, 117 };
            var rngs_10000 = new int[] { 69, 70, 70, 71, 71, 71, 70, 68, 66, 63, 58, 53, 47, 40, 33, 26, 18, 11, 3, 0 };
            var hobs_5000 = new int[] { 0, 10, 22, 35, 46, 57, 68, 78, 90, 99, 106, 113, 119, 125, 131, 135, 138, 141, 143, 144, 145, 146 };
            var rngs_5000 = new int[] { 88, 88, 89, 90, 90, 90, 89, 88, 85, 81, 78, 74, 69, 63, 55, 48, 41, 34, 26, 17, 9, 0 };
            var hobs_2000 = new int[] { 0, 7, 16, 26, 36, 45, 55, 67, 77, 86, 96, 104, 113, 121, 130, 138, 144, 151, 156, 161, 167, 172, 177, 180, 183, 186, 188, 190, 191, 192 };
            var rngs_2000 = new int[] { 119, 119, 120, 121, 122, 122, 122, 122, 122, 121, 120, 119, 117, 115, 112, 108, 105, 100, 94, 88, 81, 73, 64, 56, 48, 40, 30, 21, 11, 0 };
            var hobs_1000 = new int[] { 0, 7, 17, 27, 37, 47, 58, 71, 82, 94, 104, 115, 126, 135, 144, 153, 161, 170, 178, 184, 191, 196, 202, 206, 210, 214, 218, 222, 225, 228, 230, 232, 234, 236, 237, 238, 239 };
            var rngs_1000 = new int[] { 154, 154, 154, 155, 155, 156, 156, 157, 157, 158, 157, 156, 154, 152, 149, 146, 142, 137, 131, 127, 121, 115, 108, 102, 96, 91, 83, 75, 67, 59, 52, 43, 33, 24, 15, 8, 0 };
            var hobs_500 = new int[] { 0, 9, 19, 29, 39, 50, 61, 76, 89, 103, 118, 130, 142, 153, 166, 179, 191, 199, 209, 221, 229, 236, 244, 250, 256, 261, 266, 271, 277, 281, 284, 286, 289, 290, 291 };
            var rngs_500 = new int[] { 193, 194, 195, 196, 198, 199, 200, 202, 203, 203, 204, 203, 202, 200, 198, 194, 189, 184, 179, 170, 163, 154, 144, 134, 125, 117, 107, 96, 84, 73, 60, 48, 33, 18, 0 };
            var hobs_200 = new int[] { 0, 13, 26, 39, 54, 69, 86, 103, 119, 136, 156, 177, 195, 209, 227, 240, 249, 258, 266, 274, 283, 290, 302, 310, 317, 324, 331, 338, 343, 349, 356, 362, 367, 372, 376, 380, 382, 386, 387, 390 };
            var rngs_200 = new int[] { 264, 264, 265, 265, 266, 266, 267, 268, 269, 269, 270, 270, 269, 268, 265, 261, 257, 253, 249, 243, 238, 231, 221, 213, 204, 195, 186, 175, 166, 155, 142, 131, 121, 107, 94, 78, 64, 44, 32, 0 };
            var hobs_100 = new int[] { 0, 10, 25, 42, 60, 78, 96, 113, 131, 152, 175, 194, 212, 230, 249, 269, 286, 299, 313, 326, 338, 349, 360, 368, 377, 386, 394, 400, 407, 415, 422, 429, 438, 447, 455, 462, 468, 474, 478, 485, 489, 493, 496, 498, 500, 501 };
            var rngs_100 = new int[] { 342, 342, 343, 345, 345, 346, 347, 348, 350, 351, 353, 354, 355, 355, 355, 354, 353, 351, 349, 344, 339, 332, 324, 317, 307, 297, 287, 279, 270, 258, 247, 237, 223, 206, 191, 176, 163, 149, 135, 117, 100, 81, 58, 39, 17, 0 };
            var hobs_50 = new int[] { 0, 19, 50, 90, 136, 174, 209, 244, 279, 319, 346, 371, 391, 406, 427, 447, 459, 472, 481, 490, 504, 516, 527, 537, 548, 558, 568, 579, 588, 598, 606, 613, 620, 625, 630, 633, 635, 637, 638 };
            var rngs_50 = new int[] { 459, 459, 461, 463, 465, 469, 473, 478, 483, 489, 492, 493, 492, 490, 484, 474, 463, 442, 427, 406, 386, 365, 347, 329, 310, 290, 270, 250, 227, 202, 181, 159, 138, 115, 90, 68, 46, 20, 0 };
            var hobs_30 = new int[] { 0, 24, 54, 84, 114, 143, 179, 215, 250, 292, 328, 365, 403, 441, 476, 502, 527, 552, 574, 587, 589, 591, 596, 612, 628, 647, 665, 685, 703, 721, 736, 747, 758, 766, 773, 779, 782, 784 };
            var rngs_30 = new int[] { 592, 593, 593, 594, 594, 596, 597, 600, 604, 609, 612, 618, 624, 631, 638, 642, 642, 640, 628, 609, 585, 557, 524, 486, 453, 421, 392, 353, 319, 280, 244, 214, 180, 141, 107, 65, 21, 0 };
            var hobs_20 = new int[] { 0, 34, 84, 130, 176, 223, 274, 319, 358, 399, 427, 458, 485, 512, 537, 566, 597, 627, 651, 673, 687, 694, 692, 683, 674, 672, 672, 677, 685, 697, 713, 730, 748, 764, 781, 801, 814, 827, 844, 858, 870, 881, 892, 898, 906, 912, 919, 922, 924 };
            var rngs_20 = new int[] { 714, 719, 727, 737, 747, 757, 771, 782, 795, 812, 826, 843, 860, 879, 898, 914, 922, 919, 907, 887, 860, 826, 788, 757, 729, 704, 686, 662, 639, 612, 586, 559, 533, 508, 478, 446, 419, 394, 358, 327, 297, 269, 232, 205, 165, 124, 68, 26, 0 };
            var hobs_15 = new int[] { 0, 27, 67, 114, 160, 209, 250, 294, 319, 359, 398, 434, 459, 488, 515, 538, 565, 594, 624, 650, 676, 692, 711, 726, 739, 750, 761, 771, 776, 779, 778, 773, 764, 751, 738, 731, 733, 740, 750, 765, 780, 798, 815, 831, 845, 861, 874, 889, 907, 921, 935, 948, 958, 968, 980, 1000, 1004, 1017, 1033, 1041 };
            var rngs_15 = new int[] { 818, 827, 840, 858, 873, 893, 912, 933, 946, 974, 1002, 1033, 1057, 1083, 1111, 1133, 1158, 1178, 1193, 1196, 1191, 1183, 1173, 1161, 1146, 1129, 1109, 1082, 1058, 1028, 990, 956, 923, 892, 865, 838, 811, 784, 757, 727, 698, 668, 640, 615, 589, 563, 539, 512, 481, 452, 425, 397, 377, 352, 331, 277, 215, 153, 74, 0 };
            var hobs_10 = new int[] { 0, 30, 73, 115, 151, 191, 235, 269, 312, 345, 380, 412, 442, 471, 500, 528, 558, 592, 627, 658, 692, 724, 749, 767, 785, 805, 820, 835, 851, 862, 872, 881, 888, 893, 896, 894, 888, 881, 872, 859, 850, 846, 848, 852, 858, 867, 880, 891, 904, 916, 926, 941, 953, 965, 980, 989, 1000, 1017, 1045, 1074, 1103, 1136, 1169, 1190, 1215, 1236, 1244, 1252, 1260 };
            var rngs_10 = new int[] { 1024, 1037, 1056, 1074, 1092, 1112, 1133, 1151, 1173, 1193, 1214, 1238, 1261, 1285, 1312, 1336, 1360, 1387, 1412, 1430, 1447, 1455, 1455, 1450, 1443, 1433, 1422, 1407, 1388, 1370, 1348, 1320, 1291, 1265, 1246, 1227, 1196, 1173, 1148, 1119, 1097, 1065, 1047, 1026, 1007, 983, 957, 934, 911, 890, 874, 852, 833, 814, 795, 778, 765, 740, 690, 636, 583, 512, 450, 380, 293, 211, 128, 58, 0 };
            var hobs_8 = new int[] { 0, 83, 161, 256, 364, 459, 587, 661, 744, 847, 930, 975, 996, 1004, 983, 950, 934, 942, 967, 1029, 1095, 1145, 1198, 1244, 1289, 1314, 1351, 1384, 1401, 1417 };
            var rngs_8 = new int[] { 1124, 1169, 1223, 1277, 1347, 1409, 1492, 1558, 1636, 1694, 1694, 1645, 1591, 1550, 1483, 1376, 1289, 1194, 1103, 1012, 909, 831, 740, 649, 550, 496, 393, 248, 145, 0 };
            var hobs_6 = new int[] { 0, 62, 120, 202, 273, 364, 434, 500, 595, 686, 777, 888, 979, 1041, 1083, 1095, 1079, 1054, 1025, 1025, 1033, 1074, 1145, 1227, 1310, 1368, 1421, 1488, 1537, 1570, 1570, 1603, 1628, 1645, 1653, 1653 };
            var rngs_6 = new int[] { 1339, 1393, 1438, 1500, 1554, 1624, 1682, 1740, 1822, 1893, 1959, 2012, 2017, 1988, 1926, 1843, 1756, 1669, 1570, 1508, 1450, 1360, 1240, 1132, 1008, 926, 847, 723, 603, 512, 492, 384, 256, 124, 37, 0 };
            var hobs_4 = new int[] { 0, 37, 87, 153, 244, 331, 421, 529, 661, 777, 905, 988, 1070, 1132, 1186, 1231, 1240, 1223, 1198, 1182, 1174, 1194, 1260, 1326, 1413, 1521, 1612, 1694, 1781, 1860, 1913, 1971, 2017, 2050, 2074, 2087, 2087 };
            var rngs_4 = new int[] { 1665, 1702, 1764, 1847, 1946, 2045, 2136, 2244, 2364, 2459, 2554, 2599, 2624, 2620, 2591, 2512, 2438, 2331, 2227, 2153, 2050, 1942, 1798, 1686, 1570, 1426, 1310, 1182, 1058, 926, 810, 657, 496, 343, 182, 62, 0 };
            var hobs_2 = new int[] { 0, 25, 58, 136, 186, 227, 293, 368, 434, 479, 529, 591, 632, 686, 740, 798, 851, 921, 1017, 1099, 1198, 1264, 1318, 1372, 1413, 1430, 1429, 1421, 1422, 1438, 1508, 1587, 1702, 1818, 1988, 2190, 2302, 2426, 2492, 2517, 2612, 2698, 2793, 2893, 2971, 3041, 3079, 3103, 3136, 3157, 3178, 3190, 3202, 3211 };
            var rngs_2 = new int[] { 2558, 2616, 2682, 2847, 2942, 3033, 3136, 3281, 3380, 3467, 3545, 3632, 3702, 3781, 3868, 3942, 4021, 4099, 4182, 4207, 4182, 4136, 4079, 3992, 3901, 3777, 3649, 3525, 3401, 3264, 3116, 3000, 2876, 2752, 2579, 2364, 2236, 2091, 2000, 1971, 1839, 1707, 1537, 1351, 1161, 950, 793, 682, 545, 442, 302, 182, 70, 0 };
            var hobs_1 = new int[] { 0, 58, 140, 219, 322, 405, 496, 579, 678, 810, 888, 971, 1083, 1165, 1260, 1388, 1533, 1665, 1727, 1802, 1864, 1888, 1913, 1921, 1922, 1938, 2004, 2140, 2355, 2512, 2785, 3012, 3211, 3335, 3525, 3702, 3764, 3909, 4017, 4157, 4236, 4318, 4409, 4463, 4521, 4632, 4702, 4781, 4860, 4897, 4934, 4963, 4992, 5012, 5008, 5070 };
            var rngs_1 = new int[] { 3860, 3996, 4248, 4475, 4744, 4996, 5236, 5471, 5719, 6037, 6219, 6397, 6583, 6715, 6835, 6946, 7021, 7021, 6979, 6872, 6698, 6512, 6256, 6021, 5781, 5558, 5364, 5149, 4913, 4756, 4508, 4293, 4107, 3975, 3785, 3591, 3508, 3343, 3207, 3008, 2884, 2756, 2612, 2500, 2405, 2157, 2008, 1764, 1504, 1335, 1149, 942, 760, 612, 599, 0 };

            rngs.Add(1, rngs_1);
            rngs.Add(2, rngs_2);
            rngs.Add(4, rngs_4);
            rngs.Add(6, rngs_6);
            rngs.Add(8, rngs_8);
            rngs.Add(10, rngs_10);
            rngs.Add(15, rngs_15);
            rngs.Add(20, rngs_20);
            rngs.Add(30, rngs_30);
            rngs.Add(50, rngs_50);
            rngs.Add(100, rngs_100);
            rngs.Add(200, rngs_200);
            rngs.Add(500, rngs_500);
            rngs.Add(1000, rngs_1000);
            rngs.Add(2000, rngs_2000);
            rngs.Add(5000, rngs_5000);
            rngs.Add(10000, rngs_10000);

            hobs.Add(1, hobs_1);
            hobs.Add(2, hobs_2);
            hobs.Add(4, hobs_4);
            hobs.Add(6, hobs_6);
            hobs.Add(8, hobs_8);
            hobs.Add(10, hobs_10);
            hobs.Add(15, hobs_15);
            hobs.Add(20, hobs_20);
            hobs.Add(30, hobs_30);
            hobs.Add(50, hobs_50);
            hobs.Add(100, hobs_100);
            hobs.Add(200, hobs_200);
            hobs.Add(500, hobs_500);
            hobs.Add(1000, hobs_1000);
            hobs.Add(2000, hobs_2000);
            hobs.Add(5000, hobs_5000);
            hobs.Add(10000, hobs_10000);

            rngs_sm.Add(1, new int[55] { 3860, 3996, 4248, 4475, 4744, 4996, 5236, 5471, 5719, 6037, 6219, 6397, 6583, 6715, 6835, 6946, 7021, 7021, 6979, 6872, 6698, 6512, 6256, 6021, 5781, 5558, 5364, 5149, 4913, 4756, 4508, 4293, 4107, 3975, 3785, 3591, 3508, 3343, 3207, 3008, 2884, 2756, 2612, 2500, 2405, 2157, 2008, 1764, 1504, 1335, 1149, 942, 760, 612, 0 });
            rngs_sm.Add(2, new int[51] { 2558, 2616, 2682, 2847, 2942, 3033, 3136, 3281, 3380, 3467, 3545, 3632, 3702, 3781, 3868, 3942, 4021, 4099, 4182, 4207, 4182, 4136, 4079, 3992, 3901, 3777, 3264, 3116, 3000, 2876, 2752, 2579, 2364, 2236, 2091, 2000, 1971, 1839, 1707, 1537, 1351, 1161, 950, 793, 682, 545, 442, 302, 182, 70, 0 });
            rngs_sm.Add(4, new int[31] { 1665, 1702, 1764, 1847, 1946, 2045, 2136, 2244, 2364, 2459, 2554, 2599, 2624, 2620, 2591, 2512, 2438, 1798, 1686, 1570, 1426, 1310, 1182, 1058, 926, 810, 657, 496, 343, 182, 62 });
            rngs_sm.Add(6, new int[28] { 1339, 1393, 1438, 1500, 1554, 1624, 1682, 1740, 1822, 1893, 1959, 2012, 2017, 1988, 1926, 1843, 1240, 1132, 1008, 926, 847, 723, 603, 512, 384, 256, 124, 37 });
            rngs_sm.Add(8, new int[25] { 1124, 1169, 1223, 1277, 1347, 1409, 1492, 1558, 1636, 1694, 1694, 1645, 1591, 1550, 1012, 909, 831, 740, 649, 550, 496, 393, 248, 145, 0 });
            rngs_sm.Add(10, new int[56] { 1024, 1037, 1056, 1074, 1092, 1112, 1133, 1151, 1173, 1193, 1214, 1238, 1261, 1285, 1312, 1336, 1360, 1387, 1412, 1430, 1447, 1455, 1455, 1450, 1443, 1433, 1422, 1407, 1388, 1370, 1348, 1320, 1291, 1265, 1246, 911, 890, 874, 852, 833, 814, 795, 778, 765, 740, 690, 636, 583, 512, 450, 380, 293, 211, 128, 58, 0 });
            rngs_sm.Add(15, new int[50] { 818, 827, 840, 858, 873, 893, 912, 933, 946, 974, 1002, 1033, 1057, 1083, 1111, 1133, 1158, 1178, 1193, 1196, 1191, 1183, 1173, 1161, 1146, 1129, 1109, 1082, 1058, 1028, 698, 668, 640, 615, 589, 563, 539, 512, 481, 452, 425, 397, 377, 352, 331, 277, 215, 153, 74, 0 });
            rngs_sm.Add(20, new int[42] { 714, 719, 727, 737, 747, 757, 771, 782, 795, 812, 826, 843, 860, 879, 898, 914, 922, 919, 907, 887, 860, 826, 612, 586, 559, 533, 508, 478, 446, 419, 394, 358, 327, 297, 269, 232, 205, 165, 124, 68, 26, 0 });
            rngs_sm.Add(30, new int[38] { 592, 593, 593, 594, 594, 596, 597, 600, 604, 609, 612, 618, 624, 631, 638, 642, 642, 640, 628, 609, 585, 557, 524, 486, 453, 421, 392, 353, 319, 280, 244, 214, 180, 141, 107, 65, 21, 0 });
            rngs_sm.Add(50, new int[39] { 459, 459, 461, 463, 465, 469, 473, 478, 483, 489, 492, 493, 492, 490, 484, 474, 463, 442, 427, 406, 386, 365, 347, 329, 310, 290, 270, 250, 227, 202, 181, 159, 138, 115, 90, 68, 46, 20, 0 });
            rngs_sm.Add(100, new int[46] { 342, 342, 343, 345, 345, 346, 347, 348, 350, 351, 353, 354, 355, 355, 355, 354, 353, 351, 349, 344, 339, 332, 324, 317, 307, 297, 287, 279, 270, 258, 247, 237, 223, 206, 191, 176, 163, 149, 135, 117, 100, 81, 58, 39, 17, 0 });
            rngs_sm.Add(200, new int[40] { 264, 264, 265, 265, 266, 266, 267, 268, 269, 269, 270, 270, 269, 268, 265, 261, 257, 253, 249, 243, 238, 231, 221, 213, 204, 195, 186, 175, 166, 155, 142, 131, 121, 107, 94, 78, 64, 44, 32, 0 });
            rngs_sm.Add(500, new int[35] { 193, 194, 195, 196, 198, 199, 200, 202, 203, 203, 204, 203, 202, 200, 198, 194, 189, 184, 179, 170, 163, 154, 144, 134, 125, 117, 107, 96, 84, 73, 60, 48, 33, 18, 0 });
            rngs_sm.Add(1000, new int[37] { 154, 154, 154, 155, 155, 156, 156, 157, 157, 158, 157, 156, 154, 152, 149, 146, 142, 137, 131, 127, 121, 115, 108, 102, 96, 91, 83, 75, 67, 59, 52, 43, 33, 24, 15, 8, 0 });
            rngs_sm.Add(2000, new int[30] { 119, 119, 120, 121, 122, 122, 122, 122, 122, 121, 120, 119, 117, 115, 112, 108, 105, 100, 94, 88, 81, 73, 64, 56, 48, 40, 30, 21, 11, 0 });
            rngs_sm.Add(5000, new int[22] { 88, 88, 89, 90, 90, 90, 89, 88, 85, 81, 78, 74, 69, 63, 55, 48, 41, 34, 26, 17, 9, 0 });
            rngs_sm.Add(10000, new int[18] { 69, 70, 70, 71, 71, 71, 70, 68, 66, 63, 58, 53, 47, 40, 33, 26, 18, 11 });

            hobs_sm.Add(1, new int[55] { 0, 58, 140, 219, 322, 405, 496, 579, 678, 810, 888, 971, 1083, 1165, 1260, 1388, 1533, 1665, 1727, 1802, 1864, 1888, 1913, 1921, 1922, 1938, 2004, 2140, 2355, 2512, 2785, 3012, 3211, 3335, 3525, 3702, 3764, 3909, 4017, 4157, 4236, 4318, 4409, 4463, 4521, 4632, 4702, 4781, 4860, 4897, 4934, 4963, 4992, 5012, 5070 });
            hobs_sm.Add(2, new int[51] { 0, 25, 58, 136, 186, 227, 293, 368, 434, 479, 529, 591, 632, 686, 740, 798, 851, 921, 1017, 1099, 1198, 1264, 1318, 1372, 1413, 1430, 1438, 1508, 1587, 1702, 1818, 1988, 2190, 2302, 2426, 2492, 2517, 2612, 2698, 2793, 2893, 2971, 3041, 3079, 3103, 3136, 3157, 3178, 3190, 3202, 3211 });
            hobs_sm.Add(4, new int[31] { 0, 37, 87, 153, 244, 331, 421, 529, 661, 777, 905, 988, 1070, 1132, 1186, 1231, 1240, 1260, 1326, 1413, 1521, 1612, 1694, 1781, 1860, 1913, 1971, 2017, 2050, 2074, 2087 });
            hobs_sm.Add(6, new int[28] { 0, 62, 120, 202, 273, 364, 434, 500, 595, 686, 777, 888, 979, 1041, 1083, 1095, 1145, 1227, 1310, 1368, 1421, 1488, 1537, 1570, 1603, 1628, 1645, 1653 });
            hobs_sm.Add(8, new int[25] { 0, 83, 161, 256, 364, 459, 587, 661, 744, 847, 930, 975, 996, 1004, 1029, 1095, 1145, 1198, 1244, 1289, 1314, 1351, 1384, 1401, 1417 });
            hobs_sm.Add(10, new int[56] { 0, 30, 73, 115, 151, 191, 235, 269, 312, 345, 380, 412, 442, 471, 500, 528, 558, 592, 627, 658, 692, 724, 749, 767, 785, 805, 820, 835, 851, 862, 872, 881, 888, 893, 896, 904, 916, 926, 941, 953, 965, 980, 989, 1000, 1017, 1045, 1074, 1103, 1136, 1169, 1190, 1215, 1236, 1244, 1252, 1260 });
            hobs_sm.Add(15, new int[50] { 0, 27, 67, 114, 160, 209, 250, 294, 319, 359, 398, 434, 459, 488, 515, 538, 565, 594, 624, 650, 676, 692, 711, 726, 739, 750, 761, 771, 776, 779, 780, 798, 815, 831, 845, 861, 874, 889, 907, 921, 935, 948, 958, 968, 980, 1000, 1004, 1017, 1033, 1041 });
            hobs_sm.Add(20, new int[42] { 0, 34, 84, 130, 176, 223, 274, 319, 358, 399, 427, 458, 485, 512, 537, 566, 597, 627, 651, 673, 687, 694, 697, 713, 730, 748, 764, 781, 801, 814, 827, 844, 858, 870, 881, 892, 898, 906, 912, 919, 922, 924 });
            hobs_sm.Add(30, new int[38] { 0, 24, 54, 84, 114, 143, 179, 215, 250, 292, 328, 365, 403, 441, 476, 502, 527, 552, 574, 587, 589, 591, 596, 612, 628, 647, 665, 685, 703, 721, 736, 747, 758, 766, 773, 779, 782, 784 });
            hobs_sm.Add(50, new int[39] { 0, 19, 50, 90, 136, 174, 209, 244, 279, 319, 346, 371, 391, 406, 427, 447, 459, 472, 481, 490, 504, 516, 527, 537, 548, 558, 568, 579, 588, 598, 606, 613, 620, 625, 630, 633, 635, 637, 638 });
            hobs_sm.Add(100, new int[46] { 0, 10, 25, 42, 60, 78, 96, 113, 131, 152, 175, 194, 212, 230, 249, 269, 286, 299, 313, 326, 338, 349, 360, 368, 377, 386, 394, 400, 407, 415, 422, 429, 438, 447, 455, 462, 468, 474, 478, 485, 489, 493, 496, 498, 500, 501 });
            hobs_sm.Add(200, new int[40] { 0, 13, 26, 39, 54, 69, 86, 103, 119, 136, 156, 177, 195, 209, 227, 240, 249, 258, 266, 274, 283, 290, 302, 310, 317, 324, 331, 338, 343, 349, 356, 362, 367, 372, 376, 380, 382, 386, 387, 390 });
            hobs_sm.Add(500, new int[35] { 0, 9, 19, 29, 39, 50, 61, 76, 89, 103, 118, 130, 142, 153, 166, 179, 191, 199, 209, 221, 229, 236, 244, 250, 256, 261, 266, 271, 277, 281, 284, 286, 289, 290, 291 });
            hobs_sm.Add(1000, new int[37] { 0, 7, 17, 27, 37, 47, 58, 71, 82, 94, 104, 115, 126, 135, 144, 153, 161, 170, 178, 184, 191, 196, 202, 206, 210, 214, 218, 222, 225, 228, 230, 232, 234, 236, 237, 238, 239 });
            hobs_sm.Add(2000, new int[30] { 0, 7, 16, 26, 36, 45, 55, 67, 77, 86, 96, 104, 113, 121, 130, 138, 144, 151, 156, 161, 167, 172, 177, 180, 183, 186, 188, 190, 191, 192 });
            hobs_sm.Add(5000, new int[22] { 0, 10, 22, 35, 46, 57, 68, 78, 90, 99, 106, 113, 119, 125, 131, 135, 138, 141, 143, 144, 145, 146 });
            hobs_sm.Add(10000, new int[18] { 0, 10, 22, 33, 44, 54, 65, 73, 79, 88, 94, 99, 104, 108, 111, 114, 116, 117 });

            //Eq. 2.4 - maximum overpressure at 0 feet; input is scaled range; output in psi
            eq["2-4"] = new Dictionary<string, double[]>();
            eq["2-4"]["xmin"] = new double[] { 0.0472 };
            eq["2-4"]["xmax"] = new double[] { 4.82 };
            eq["2-4"]["args"] = new double[] { -0.1877932, -1.3986162, 0.3255743, -0.0267036 };
            //eq["2-4"]["desc"] = "Max overpressure (surface): psi from scaled range";

            //Eq. 2.5 - maximum overpressure at 0 feet; input is psi; output in scaled range
            eq["2-5"] = new Dictionary<string, double[]>();
            eq["2-5"]["xmin"] = new double[] { 0.1 };
            eq["2-5"]["xmax"] = new double[] { 200 };
            eq["2-5"]["args"] = new double[] { -0.1307982, -0.6836211, 0.1091296, -0.0167348 };
            //eq["2-5"]["desc"] = "Max overpressure (surface): scaled range from psi";

            //Eq. 2.19 - maximum overpressure at 100 feet; input is psi; output in scaled range
            eq["2-19"] = new Dictionary<string, double[]>();
            eq["2-19"]["xmin"] = new double[] { 1 };
            eq["2-19"]["xmax"] = new double[] { 200 };
            eq["2-19"]["args"] = new double[] { -0.0985896, -.6788230, 0.0846268, -.0089153 };
            //eq["2-19"]["desc"] = "Max overpressure (100 ft): scaled range from psi";

            //Eq. 2.25 - maximum overpressure at 200 feet; input is psi; output in scaled range
            eq["2-25"] = new Dictionary<string, double[]>();
            eq["2-25"]["xmin"] = new double[] { 1 };
            eq["2-25"]["xmax"] = new double[] { 200 };
            eq["2-25"]["args"] = new double[] { -0.0564384, -0.7063068, 0.0838300, -0.0057337 };
            //eq["2-25"]["desc"] = "Max overpressure (200 ft): scaled range from psi";

            //Eq. 2.31 - maximum overpressure at 300 feet; input is psi; output in scaled range
            eq["2-31"] = new Dictionary<string, double[]>();
            eq["2-31"]["xmin"] = new double[] { 1 };
            eq["2-31"]["xmax"] = new double[] { 100 };
            eq["2-31"]["args"] = new double[] { -0.0324052, -0.6430061, -.0307184, 0.0375190 };
            //eq["2-31"]["desc"] = "Max overpressure (300 ft): scaled range from psi";

            //Eq. 2.37 - maximum overpressure at 400 feet; input is psi; output in scaled range
            eq["2-37"] = new Dictionary<string, double[]>();
            eq["2-37"]["xmin"] = new double[] { 1 };
            eq["2-37"]["xmax"] = new double[] { 50 };
            eq["2-37"]["args"] = new double[] { -0.0083104, -0.6809590, 0.0443969, 0.0032291 };
            //eq["2-37"]["desc"] = "Max overpressure (400 ft): scaled range from psi";

            //Eq. 2.43 - maximum overpressure at 500 feet; input is psi; output in scaled range
            eq["2-43"] = new Dictionary<string, double[]>();
            eq["2-43"]["xmin"] = new double[] { 1 };
            eq["2-43"]["xmax"] = new double[] { 50 };
            eq["2-43"]["args"] = new double[] { 0.0158545, -0.7504681, 0.1812493, -0.0573264 };
            //eq["2-43"]["desc"] = "Max overpressure (500 ft): scaled range from psi";

            //Eq. 2.49 - maximum overpressure at 600 feet; input is psi; output in scaled range
            eq["2-49"] = new Dictionary<string, double[]>();
            eq["2-49"]["xmin"] = new double[] { 1 };
            eq["2-49"]["xmax"] = new double[] { 30 };
            eq["2-49"]["args"] = new double[] { 0.0382755, -0.8763984, -0.4701227, -0.02046373 };
            //eq["2-49"]["desc"] = "Max overpressure (600 ft): scaled range from psi";

            //Eq. 2.55 - maximum overpressure at 700 feet; input is psi; output in scaled range
            eq["2-55"] = new Dictionary<string, double[]>();
            eq["2-55"]["xmin"] = new double[] { 1 };
            eq["2-55"]["xmax"] = new double[] { 20 };
            eq["2-55"]["args"] = new double[] { 0.0468997, -0.7764501, 0.3312436, -.1647522 };
            //eq["2-55"]["desc"] = "Max overpressure (700 ft): scaled range from psi";

            //Eq. 2.61 - maximum overpressure at optimum blast altitude; input is psi; output in scaled range
            eq["2-61"] = new Dictionary<string, double[]>();
            eq["2-61"]["xmin"] = new double[] { 1 };
            eq["2-61"]["xmax"] = new double[] { 200 };
            eq["2-61"]["args"] = new double[] { 0.1292768, -0.7227471, 0.0147366, 0.0135239 };
            //eq["2-61"]["desc"] = "Max overpressure (OBH): scaled range from psi";

            //Eq. 2.60 - maximum overpressure at optimum altitude of burst; input is scaled range; output in psi
            eq["2-60"] = new Dictionary<string, double[]>();
            eq["2-60"]["xmin"] = new double[] { 0.0508 };
            eq["2-60"]["xmax"] = new double[] { 1.35 };
            eq["2-60"]["args"] = new double[] { 0.1829156, -1.4114030, -0.0373825, -0.1635453 };
            //eq["2-60"]["desc"] = "Max overpressure (OBH): psi from scaled range";

            //Eq. 2.6 - maximum dynamic pressure at 0 feet; input is scaled range; output in psi
            eq["2-6"] = new Dictionary<string, double[]>();
            eq["2-6"]["xmin"] = new double[] { 0.0615 };
            eq["2-6"]["xmax"] = new double[] { 4.73 };
            eq["2-6"]["args"] = new double[] { -1.9790344, -2.7267144, 0.5250615, -0.1160756 };
            //eq["2-6"]["desc"] = "Max dynamic pressure (surface): psi from scaled range";

            //Eq. 2.62 - maximum dynamic pressure at optimum altitude of burst; input is scaled range; output in psi
            eq["2-62"] = new Dictionary<string, double[]>();
            eq["2-62"]["xmin"] = new double[] { 0.154 };
            eq["2-62"]["xmax"] = new double[] { 1.37 };
            eq["2-62"]["args"] = new double[] { 1.2488468, -2.7368746 };
            //eq["2-62"]["desc"] = "Max dynamic pressure (OBH): psi from scaled range";

            //Eq. 2.64 - maximum dynamic pressure at optimum altitude of burst; input is scaled range; output in psi
            eq["2-64"] = new Dictionary<string, double[]>();
            eq["2-64"]["xmin"] = new double[] { 0.0932 };
            eq["2-64"]["xmax"] = new double[] { 0.154 };
            eq["2-64"]["args"] = new double[] { -3.8996912, -6.0108828 };
            //eq["2-64"]["desc"] = "Max dynamic pressure (OBH): psi from scaled range";

            //Eq. 2.8 - duration of positive overpressure at 0 feet; input is scaled range; output in sec
            eq["2-8"] = new Dictionary<string, double[]>();
            eq["2-8"]["xmin"] = new double[] { 0.0677 };
            eq["2-8"]["xmax"] = new double[] { 0.740 };
            eq["2-8"]["args"] = new double[] { -0.1739890, 0.5265382, -.0772505, 0.0654855 };
            //eq["2-8"]["desc"] = "Duration of positive overpressure (surface): sec from scaled range";

            //Eq. 2.12 - blast wave arrival time at 0 feet; input is scaled range; output in sec
            eq["2-12"] = new Dictionary<string, double[]>();
            eq["2-12"]["xmin"] = new double[] { 0.0570 };
            eq["2-12"]["xmax"] = new double[] { 1.10 };
            eq["2-12"]["args"] = new double[] { 0.6078753, 1.1039021, -0.2836934, 0.1006855 };

            //Eq. 2.16 - maximum wind velocity at 0 feet; input is scaled range; output in mph
            eq["2-16"] = new Dictionary<string, double[]>();
            eq["2-16"]["xmin"] = new double[] { 0.0589 };
            eq["2-16"]["xmax"] = new double[] { 4.73 };
            eq["2-16"]["args"] = new double[] { 1.3827823, -1.3518147, 0.1841482, 0.0361427 };

            //Eq. 2.74 - maximum wind velocity at optimum burst altitude; input is scaled range; output in mph
            eq["2-74"] = new Dictionary<string, double[]>();
            eq["2-74"]["xmin"] = new double[] { 0.2568 };
            eq["2-74"]["xmax"] = new double[] { 1.4 };
            eq["2-74"]["args"] = new double[] { 1.7110032, -1.2000278, 0.8182584, 1.0652528 };

            //Eq. 2.76 - maximum wind velocity at optimum burst altitude; input is scaled range; output in mph
            eq["2-76"] = new Dictionary<string, double[]>();
            eq["2-76"]["xmin"] = new double[] { 0.0762 };
            eq["2-76"]["xmax"] = new double[] { 0.2568 };
            eq["2-76"]["args"] = new double[] { 3.8320701, 5.6357427, 6.6091754, 1.5690375 };

            /* OPTIMUM HEIGHT OF BURST */

            //Eq. 2.78 - optimum altitude of burst for given overpressure; input is maximum overpressure; output is scaled altitude
            eq["2-78"] = new Dictionary<string, double[]>();
            eq["2-78"]["xmin"] = new double[] { 1 };
            eq["2-78"]["xmax"] = new double[] { 200 };
            eq["2-78"]["args"] = new double[] { 3.2015016, -0.3263444 };

            //Eq. 2.79 - optimum altitude of burst to maximize overpressure; input is scaled range; output is scaled altitude
            eq["2-79"] = new Dictionary<string, double[]>();
            eq["2-79"]["xmin"] = new double[] { 0.0512 };
            eq["2-79"]["xmax"] = new double[] { 1.35 };
            eq["2-79"]["args"] = new double[] { 3.1356018, 0.3833517, -0.1159125 };

            /* THERMAL RADIATION */

            //Eq. 2.106 - thermal radiation, input is slant range, for airburst, output is Q(1/W); for surface, input is range, output is Q(1/.7W)
            eq["2-106"] = new Dictionary<string, double[]>();
            eq["2-106"]["xmin"] = new double[] { 0.05 };
            eq["2-106"]["xmax"] = new double[] { 50 };
            eq["2-106"]["args"] = new double[] { -0.0401874, -2.0823477, -0.0511744, -0.0074958 };

            //Eq. 2.108 - thermal radiation, input for airburst is Q(1/W); for surface, is Q(1/.7W); output is distance/slant distance
            eq["2-108"] = new Dictionary<string, double[]>();
            eq["2-108"]["xmin"] = new double[] { 0.0001 };
            eq["2-108"]["xmax"] = new double[] { 100 };
            eq["2-108"]["args"] = new double[] { -0.0193419, -0.4804553, -0.0055685, 0.0002013 };

            //Eq. 2.110 - thermal radiation for 1st degree burns; input is yield, output is Q (cal/cm^2)
            eq["2-110"] = new Dictionary<string, double[]>();
            eq["2-110"]["xmin"] = new double[] { 1 };
            eq["2-110"]["xmax"] = new double[] { 100000 };
            eq["2-110"]["args"] = new double[] { 0.3141555, 0.059904, 0.0007636, -0.0002015 };

            //Eq. 2.111 - thermal radiation for 2nd degree burns; input is yield, output is Q (cal/cm^2)
            eq["2-111"] = new Dictionary<string, double[]>();
            eq["2-111"]["xmin"] = new double[] { 1 };
            eq["2-111"]["xmax"] = new double[] { 100000 };
            eq["2-111"]["args"] = new double[] { 0.6025982, 0.0201394, 0.0139640, 0.0008559 };

            /* Following 5 equations derived from figure 12.65 of Glasstone and Dolan 1977 */

            // These are technically only bound between 1kt and 20 MT but the scaling looks fine enough 
            //Eq. 77-12.65-1st-50 - thermal radiation for 50% probability of an unshielded population for 1st degree burns
            //input is yield, output is Q (cal/cm^2)
            eq["77-12.65-1st-50"] = new Dictionary<string, double[]>();
            eq["77-12.65-1st-50"]["xmin"] = new double[] { 1 };
            eq["77-12.65-1st-50"]["xmax"] = new double[] { 20000 };
            eq["77-12.65-1st-50"]["args"] = new double[] { 1.93566176470914, 0.325315457507999, -0.113516274769641, 0.0300971575115961, -0.00330445814836616, 0.000129665656335876 };

            //Eq. 77-12.65-2nd-50 - thermal radiation for 50% probability of an unshielded population for 2nd degree burns
            //input is yield, output is Q (cal/cm^2)
            eq["77-12.65-2nd-50"] = new Dictionary<string, double[]>();
            eq["77-12.65-2nd-50"]["xmin"] = new double[] { 1 };
            eq["77-12.65-2nd-50"]["xmax"] = new double[] { 20000 };
            eq["77-12.65-2nd-50"]["args"] = new double[] { 4.0147058823566697E+00, 3.7180525416799937E-01, -4.5026131075683193E-02, 1.3549565337157871E-02, -1.6559848551158524E-03, 7.0380159845451207E-05 };

            //Eq. 77-12.65-3rd-50 - thermal radiation for 50% probability of an unshielded population for 3rd degree burns
            //input is yield, output is Q (cal/cm^2)
            eq["77-12.65-3rd-50"] = new Dictionary<string, double[]>();
            eq["77-12.65-3rd-50"]["xmin"] = new double[] { 1 };
            eq["77-12.65-3rd-50"]["xmax"] = new double[] { 20000 };
            eq["77-12.65-3rd-50"]["args"] = new double[] { 5.9981617647112317E+00, 5.3350791551060528E-01, -2.3435878115600033E-02, 1.0395274013807305E-02, -1.4366360115630195E-03, 6.3930657856814399E-05 };

            //Eq. 77-12.65-noharm-100 - thermal radiation for 100% probability of an unshielded population for no burns
            //input is yield, output is Q (cal/cm^2)
            eq["77-12.65-noharm-100"] = new Dictionary<string, double[]>();
            eq["77-12.65-noharm-100"]["xmin"] = new double[] { 1 };
            eq["77-12.65-noharm-100"]["xmax"] = new double[] { 20000 };
            eq["77-12.65-noharm-100"]["args"] = new double[] { 1.14705882353066, 0.124659908645308, -0.0160088216223604, 0.00359441786929512, -0.000263841056172493, 0.0000053050769836388 };

            //Eq. 77-12.65-3rd-100 - thermal radiation for 100% probability of an unshielded population for 3rd degree burns
            //input is yield, output is Q (cal/cm^2)
            eq["77-12.65-3rd-100"] = new Dictionary<string, double[]>();
            eq["77-12.65-3rd-100"]["xmin"] = new double[] { 1 };
            eq["77-12.65-3rd-100"]["xmax"] = new double[] { 20000 };
            eq["77-12.65-3rd-100"]["args"] = new double[] { 7.0018382352996857, .55437306382914320, .056501270479506649, -.015219252753643841, .0017062986685328282, -.000067950215125955893 };


            /* INITIAL NUCLEAR RADIATION */

            //Eq. 2.115 - ratio of scaling factor to yield, used for 2.114; input is yield, output is scaling factor
            eq["2-115"] = new Dictionary<string, double[]>();
            eq["2-115"]["xmin"] = new double[] { 10 };
            eq["2-115"]["xmax"] = new double[] { 20000 };
            eq["2-115"]["args"] = new double[] { -2.1343121, 5.6948378, -5.7707609, 2.7712520, -0.6206012, 0.0526380 };


        }

        //input is kt, psi, and altitude of burst (feet)
        //output is a ground range distance (feet) at which that psi would be felt
        private double range_from_psi_hob(double kt, double psi, double hob)
        {
            double scaled_hob = hob / Math.Pow(kt, 1 / 3.0);
            double range_at_1kt = range_from_psi_hob_1kt(psi, scaled_hob);
            double rrr = range_at_1kt * Math.Pow(kt, 1 / 3.0);
            return range_at_1kt * Math.Pow(kt, 1 / 3.0);
        }

        //input is psi and altitude of burst (feet)
        //returns ground range for a 1 kiloton shot for a given psi and hob
        private double range_from_psi_hob_1kt(double psi, double hob)
        {

            //if out of range, return false
            if (hob < 0) return -1; //min hob
            if (psi > 10000) return -1; //max psi
            if (psi < 1) return -1; //min psi

            int min_hob_k = 0;
            int max_hob_k = 0;

            if (hobs.ContainsKey(psi))
            {
                //if the psi is one that we have direct data for

                if (hob > hobs[psi][hobs[psi].Length - 1])
                {
                    return 0; //too high
                }

                //check the closest heights in our data
                int[] near_hobs = array_closest(hobs[psi], hob);//near_hobs是一个数组
                if (near_hobs.Length == 1)
                {
                    return rngs[psi][near_hobs[0]];
                }
                else
                {
                    min_hob_k = near_hobs[0];
                    max_hob_k = near_hobs[1];
                }

                //interpolate the desired result from the known results using smooth array
                return lerp(rngs_sm[psi][min_hob_k], hobs_sm[psi][min_hob_k], rngs_sm[psi][max_hob_k], hobs_sm[psi][max_hob_k], hob);
            }
            else
            {
                //if we don't have that psi
                //have to do some rather complicated interpolation!
                return range_from_psi_hob_1kt_interpolated(psi, hob);
            }
        }

        //function that searches an array for the closest values -- returns two indices
        private int[] array_closest(int[] arr, double val)
        {
            int lo = -1;
            int hi = -1;

            int lo_k = -1;
            int hi_k = -1;
            for (int i = 0; i < arr.Length; i++)
            //foreach (int i in arr)
            {
                if (arr[i] == val)
                {
                    return new int[] { i };
                }
                if (arr[i] <= val && (lo == -1 || lo < arr[i]))
                {
                    lo = arr[i];
                    lo_k = i;
                }
                if (arr[i] >= val && (hi == -1 || hi > arr[i]))
                {
                    hi = arr[i];
                    hi_k = i;
                }
            }
            if (hi_k != lo_k + 1)
            {
                lo_k = hi_k - 1;
                //echo "hmm... ";
            }
            return new int[] { lo_k, hi_k };

        }

        private double range_from_psi_hob_1kt_interpolated(double psi, double hob)
        {
            double result = -1;

            var h = hob;

            var psi_ = psi_find(psi);
            var p1 = psi_[0]; //OUTER
            var p2 = psi_[1]; //INNER
            if (h <= 0)
            {
                //easy case
                result = lerp(rngs[p1][0], p1, rngs[p2][0], p2, psi);
                return result;
            }

            //first check if it is out of bounds
            var max_hob_outer = hobs[p1][hobs[p1].Length - 1];
            var max_hob_inner = hobs[p2][hobs[p2].Length - 1];
            var max_hob_lerp = lerp(max_hob_outer, p1, max_hob_inner, p2, psi);

            if (h > max_hob_lerp)
            {
                return 0;
            }

            //start on the hard case...

            //the proportion between the two PSIs
            var proportion = lerp(0, p2, 1, p1, psi);

            var near_hobs = array_closest(hobs[p1], h);

            //search start index
            var outer_index = near_hobs[0];
            var search_direction = 1;

            var intercept = getInterpolatedPosition(p2, p1, outer_index);
            if (intercept==null)
            {
                return 0;
            }

            var h_low_index=0.0; var h_low_prop = 0.0; var r_low_prop = 0.0;
            var h_high_index = 0.0; var h_high_prop = 0.0; var r_high_prop = 0.0;
            

            while (intercept.lat < h)
            {
                var rng_at_prop = lerp(rngs[p1][outer_index], 1, intercept.lng, 0, proportion);
                var hob_at_prop = lerp(hobs[p1][outer_index], 1, intercept.lat, 0, proportion);
                if (hob_at_prop < h)
                {
                    if (outer_index > h_low_index || h_low_index >0)
                    {
                        h_low_index = outer_index;
                        h_low_prop = hob_at_prop;
                        r_low_prop = rng_at_prop;
                    }
                }
                if (hob_at_prop > h)
                {
                    if (h_low_index>0)
                    {
                        h_high_prop = hob_at_prop;
                        r_high_prop = rng_at_prop;
                        result = lerp(r_low_prop, h_low_prop, r_high_prop, h_high_prop, h);
                        return result;
                        break;
                    }
                }

                outer_index += search_direction;

                if ((outer_index >= hobs[p1].Length) || (outer_index < 0))
                {
                    return -1;
                    break;
                }
                else
                {
                    intercept = getInterpolatedPosition(p2, p1, outer_index);
                    if (intercept==null) return -1;
                }
            }

            if (result>0 && h_low_index>0)
            {
                var rng_at_prop = lerp(rngs[p1][outer_index], 1, intercept.lng, 0, proportion);
                var hob_at_prop = lerp(hobs[p1][outer_index], 1, intercept.lat, 0, proportion);
                h_high_prop = hob_at_prop;
                r_high_prop = rng_at_prop;
                result = lerp(r_low_prop, h_low_prop, r_high_prop, h_high_prop, h);

                return result;
            }
            else
            {
                //so low that it fails -- just take the last two measurements and use those
                var rng_at_prop = lerp(rngs[p1][outer_index], 1, intercept.lng, 0, proportion);
                var hob_at_prop = lerp(hobs[p1][outer_index], 1, intercept.lat, 0, proportion);
                h_high_prop = hob_at_prop;
                r_high_prop = rng_at_prop;
                intercept = getInterpolatedPosition(p2, p1, outer_index - 1);
                if (intercept==null) return -1;
                rng_at_prop = lerp(rngs[p1][outer_index - 1], 1, intercept.lng, 0, proportion);
                hob_at_prop = lerp(hobs[p1][outer_index - 1], 1, intercept.lat, 0, proportion);
                h_low_prop = hob_at_prop;
                r_low_prop = rng_at_prop;
                result = lerp(r_low_prop, h_low_prop, r_high_prop, h_high_prop, h);
                return result;
            }


            return 0;//failed for some reason;
        }

        private Coor getInterpolatedPosition(double inner_psi, double outer_psi, int outer_index)
        {

            var inner_index = 0; //we start from index zero (HOB = 0) and move "up." the choice of a starting index and the method of traversig the index could be optimized. 

            while (linesIntersect(
                        0, 0,
                        rngs[outer_psi][outer_index], hobs[outer_psi][outer_index],

                        rngs[inner_psi][inner_index], hobs[inner_psi][inner_index],
                        rngs[inner_psi][inner_index + 1], hobs[inner_psi][inner_index + 1]
            ) == false)
            {
                inner_index++;
                if (inner_index > rngs[inner_psi].Length || inner_index - 1 < 0)
                {
                    return null;
                }
            }
            return getLineLineIntersection(0, 0, rngs[outer_psi][outer_index], hobs[outer_psi][outer_index],
                                     rngs[inner_psi][inner_index + 1], hobs[inner_psi][inner_index + 1], rngs[inner_psi][inner_index], hobs[inner_psi][inner_index]);

        }

        //get line segment intersection point
        private Coor getLineLineIntersection(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            var det1And2 = getLineLineIntersection_det(x1, y1, x2, y2);
            var det3And4 = getLineLineIntersection_det(x3, y3, x4, y4);
            var x1LessX2 = x1 - x2;
            var y1LessY2 = y1 - y2;
            var x3LessX4 = x3 - x4;
            var y3LessY4 = y3 - y4;
            var det1Less2And3Less4 = getLineLineIntersection_det(x1LessX2, y1LessY2, x3LessX4, y3LessY4);
            if (det1Less2And3Less4 == 0)
            {
                // the denominator is zero so the lines are parallel and there's either no solution (or multiple solutions if the lines overlap) so return null.
                return null;
            }
            var x = (getLineLineIntersection_det(det1And2, x1LessX2,
                  det3And4, x3LessX4) /
                  det1Less2And3Less4);
            var y = (getLineLineIntersection_det(det1And2, y1LessY2,
                  det3And4, y3LessY4) /
                  det1Less2And3Less4);
            return new Coor(x, y);
        }

        private int getLineLineIntersection_det(int a,int b, int c, int d)
        {
            return a * d - b * c;
        }
        //determines whether two line segments intersect
        private bool linesIntersect(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
        {
            // Return false if either of the lines have zero length
            if (x1 == x2 && y1 == y2 ||
                  x3 == x4 && y3 == y4)
            {
                return false;
            }
            // Fastest method, based on Franklin Antonio's "Faster Line Segment Intersection" topic "in Graphics Gems III" book (http://www.graphicsgems.org/)
            var ax = x2 - x1;
            var ay = y2 - y1;
            var bx = x3 - x4;
            var by = y3 - y4;
            var cx = x1 - x3;
            var cy = y1 - y3;

            var alphaNumerator = by * cx - bx * cy;
            var commonDenominator = ay * bx - ax * by;
            if (commonDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > commonDenominator)
                {
                    return false;
                }
            }
            else if (commonDenominator < 0)
            {
                if (alphaNumerator > 0 || alphaNumerator < commonDenominator)
                {
                    return false;
                }
            }
            var betaNumerator = ax * cy - ay * cx;
            if (commonDenominator > 0)
            {
                if (betaNumerator < 0 || betaNumerator > commonDenominator)
                {
                    return false;
                }
            }
            else if (commonDenominator < 0)
            {
                if (betaNumerator > 0 || betaNumerator < commonDenominator)
                {
                    return false;
                }
            }
            if (commonDenominator == 0)
            {
                // This code wasn't in Franklin Antonio's method. It was added by Keith Woodward.
                // The lines are parallel.
                // Check if they're collinear.
                var y3LessY1 = y3 - y1;
                var collinearityTestForP3 = x1 * (y2 - y3) + x2 * (y3LessY1) + x3 * (y1 - y2);   // see http://mathworld.wolfram.com/Collinear.html
                                                                                                 // If p3 is collinear with p1 and p2 then p4 will also be collinear, since p1-p2 is parallel with p3-p4
                if (collinearityTestForP3 == 0)
                {
                    // The lines are collinear. Now check if they overlap.
                    if (x1 >= x3 && x1 <= x4 || x1 <= x3 && x1 >= x4 ||
                          x2 >= x3 && x2 <= x4 || x2 <= x3 && x2 >= x4 ||
                          x3 >= x1 && x3 <= x2 || x3 <= x1 && x3 >= x2)
                    {
                        if (y1 >= y3 && y1 <= y4 || y1 <= y3 && y1 >= y4 ||
                              y2 >= y3 && y2 <= y4 || y2 <= y3 && y2 >= y4 ||
                              y3 >= y1 && y3 <= y2 || y3 <= y1 && y3 >= y2)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        private int[] psi_find(double psi)
        {
            int min_psi = 0; int max_psi = 0;
            foreach (int p in psi_index)
            {
                if (p < psi)
                {
                    min_psi = p;
                }
                if (p > psi && max_psi == 0)
                {
                    max_psi = p;
                }
            }

            return new int[] { min_psi, max_psi };
        }

        //simple linear interpolation -- returns x3 for a given y3
        private double lerp(double x1, double y1, double x2, double y2, double y3)
        {
            if (y2 == y1)
            {
                return -1; //division by zero avoidance
            }
            else
            {
                return ((y2 - y3) * x1 + (y3 - y1) * x2) / (y2 - y1);
            }
        }
    }
}
