using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;

namespace ToolAPIApplication
{
    public interface IMongoService
    {
        /// <summary>
        /// 根据名称查询计算值。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        RuleBo QueryRule(string name);

    }
}
