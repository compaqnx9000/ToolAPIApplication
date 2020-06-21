using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.enums
{
    public enum DamageEnumeration : int
    {
        /// <summary>
        /// 安全
        /// </summary>
        Safe = 0,

        /// <summary>
        /// 轻度
        /// </summary>
        Light = 1,

        /// <summary>
        /// 重度
        /// </summary>
        Heavy = 2,

        /// <summary>
        /// 损毁
        /// </summary>
        Destroy = 3,
    }
}
