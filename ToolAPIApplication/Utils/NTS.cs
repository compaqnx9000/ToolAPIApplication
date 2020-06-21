using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace ToolAPIApplication.Utils
{
    public class NTS
    {

        /// <summary>
        /// 计算多个集合对象的并集，（除去当前index对象）
        /// </summary>
        /// <param name="index"></param>
        /// <param name="geoms"></param>
        public static Geometry Union_Exclude(int index,ref List<Geometry> geoms)
        {
            Geometry g = null;

            for (int i = 0; i < geoms.Count; i++)
            {
                if (i != index)
                {
                    if (g == null)
                        g = geoms[i];
                    else
                        g = g.Union(geoms[i]);
                }
            }
            return g;
        }

        public static Geometry Union(List<Geometry> geoms)
        {
            Geometry result = null;
            foreach (Geometry geom in geoms)
            {
                if (result == null)
                    result = geom;

                result = result.Union(geom);
            }
            return result;
        }

        public static Geometry Diff(Geometry src,List<Geometry> geoms)
        {
            Geometry g = src;
            foreach (Geometry geom in geoms)
            {
                g = g.Difference(geom);
            }
            return g;
        }

        public static Geometry Intersection(ref List<Geometry> geoms)
        {
            Geometry result = null;
            foreach (Geometry geom in geoms)
            {
                if (result == null)
                    result = geom;
                else
                    result = result.Intersection(geom);
            }
            return result;
        }
        // 两两求交
        public static List<Geometry> Union_2_2(ref List<Geometry> geometries)
        {
            List<Geometry> newGeometries = new List<Geometry>();

            Geometry geom = null;

            for (int i = 0; i < geometries.Count; i++)
            {
                if (geom == null)
                {
                    geom = geometries[i];
                }


                for (int j = i + 1; j < geometries.Count; j++)
                {
                    geom = geom.Buffer(0);//alan    
                    geometries[j] = geometries[j].Buffer(0);//alan

                    Geometry g = geom.Intersection(geometries[j]);
                    if (g != null)
                    {
                        //去重
                        bool repeat = false;
                        foreach (Geometry tempGemo in newGeometries)
                        {
                            if (g.Equals(tempGemo))
                            {
                                repeat = true;
                                break;
                            }
                        }
                        if(!repeat)
                        {
                            newGeometries.Add(g);
                        }

                    }
                }
                geom = null;
            }

            return newGeometries;
        }

    }
}
