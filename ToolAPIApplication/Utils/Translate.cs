using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace ToolAPIApplication.Utils
{
    public class Translate
    {

        /// <summary>
        /// 返回GeoJson格式的几何描述。
        /// </summary>
        /// <param name="geometry">几何对象。</param>
        /// <returns>GeoJson格式。</returns>
        public static string Geometry2GeoJson(Geometry geometry)
        {
            if (geometry == null)
                return "";

            NetTopologySuite.IO.GeoJsonWriter geoJsonWriter = new NetTopologySuite.IO.GeoJsonWriter();
            return geoJsonWriter.Write(geometry);//JsonWriter writer
        }

        /// <summary>
        /// 返回GeoJson格式的几何描述。
        /// </summary>
        /// <param name="geometry">几何对象。</param>
        /// <param name="level">损失程度。</param>
        /// <returns>GeoJson格式。</returns>
        public static string Geometry2GeoJson(Geometry geometry, int level)
        {
            if (geometry == null)
                return "";

            AttributesTable attributes = new AttributesTable();
            attributes.Add("level", level);
            IFeature feature = new Feature(geometry, attributes);

            NetTopologySuite.IO.GeoJsonWriter geoJsonWriter = new NetTopologySuite.IO.GeoJsonWriter();
            return geoJsonWriter.Write(feature);

        }

        /// <summary>
        /// geojson转换成几何对象。
        /// </summary>
        /// <param name="json">geojson字符串。</param>
        /// <returns>几何对象。</returns>
        public static Geometry GeoJson2Geometry(string json)
        {
            if (json != null)
            {
                NetTopologySuite.IO.GeoJsonReader reader = new NetTopologySuite.IO.GeoJsonReader();
                NetTopologySuite.Geometries.Geometry geometry = reader.Read<NetTopologySuite.Geometries.Geometry>(json);
                return geometry;
            }
            return null;
        }

        public static IFeature GeoJson2Feature(string json)
        {
            if (json != null)
            {
                NetTopologySuite.IO.GeoJsonReader reader = new NetTopologySuite.IO.GeoJsonReader();
                FeatureCollection featureCollection = reader.Read<FeatureCollection>(json);
                if (featureCollection != null)
                {
                    for (int i = 0; i < featureCollection.Count; i++)
                    {
                        IFeature jsonFeature = featureCollection[i];
                        return jsonFeature;
                    }
                }
            }
            return null;
        }

        public static Geometry BuildCircle(double lng, double lat, double r, int steps)
        {
            if (r <= 0) return null;

            int step = steps | 64;

            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < steps; i++)
            {
                coordinates.Add(destination(lng, lat, r, i * -360 / steps));
            }
            coordinates.Add(coordinates[0]);

            // 把coordinators 转换成geometry
            Coordinate[] coords = coordinates.ToArray();
            Polygon polygon = new NetTopologySuite.Geometries.Polygon(
                new LinearRing(coords));

            return polygon;
        }

        public static string BuildCircleJson(double lng, double lat, double r, int steps)
        {
            if (r <= 0) return "";

            int step = steps | 64;

            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < steps; i++)
            {
                coordinates.Add(destination(lng, lat, r, i * -360 / steps));
            }
            coordinates.Add(coordinates[0]);

            // 把coordinators 转换成geometry
            Coordinate[] coords = coordinates.ToArray();
            Polygon polygon = new NetTopologySuite.Geometries.Polygon(
                new LinearRing(coords));

            // 把geometry转换成geojson

            return Geometry2GeoJson(polygon);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="distance">公里</param>
        /// <param name="bearing"></param>
        /// <returns></returns>
        private static Coordinate destination(double lng, double lat, double distance, double bearing)
        {
            double longitude1 = degreesToRadians(lng);
            double latitude1 = degreesToRadians(lat);
            double bearing_rad = degreesToRadians(bearing);
            double radians = lengthToRadians(distance);

            double latitude2 = Math.Asin(Math.Sin(latitude1) * Math.Cos(radians) +
        Math.Cos(latitude1) * Math.Sin(radians) * Math.Cos(bearing_rad));
            double longitude2 = longitude1 + Math.Atan2(Math.Sin(bearing_rad) * Math.Sin(radians) * Math.Cos(latitude1),
                Math.Cos(radians) - Math.Sin(latitude1) * Math.Sin(latitude2));
            double new_lng = radiansToDegrees(longitude2);
            double new_lat = radiansToDegrees(latitude2);

            return new Coordinate(new_lng, new_lat);
        }

        private static double degreesToRadians(double degrees)
        {
            var radians = degrees % 360;
            return radians * Math.PI / 180;
        }

        private static double radiansToDegrees(double radians)
        {
            var degrees = radians % (2 * Math.PI);
            return degrees * 180 / Math.PI;
        }

        private static double lengthToRadians(double distance)
        {
            double factor = 6371.0088;
            return distance / factor;
        }

        //地球半径，单位米
        private const double EARTH_RADIUS = 6378137;
        /// <summary>
        /// 计算两点位置的距离，返回两点的距离，单位 米
        /// 该公式为GOOGLE提供，误差小于0.2米
        /// </summary>
        /// <param name="lat1">第一点纬度</param>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <param name="lng2">第二点经度</param>
        /// <returns></returns>
        public static double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return result;
        }

        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static double Rad(double d)
        {
            return (double)d * Math.PI / 180d;
        }
    }
}
