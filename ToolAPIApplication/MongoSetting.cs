using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication
{
    public class DBSetting
    {
        public string Database { get; set; }
        public string Collection { get; set; }
    }
    public class MongoSetting
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string IP { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Pwd { get; set; }

        public DBSetting InfoSetting { get; set; }
        public DBSetting MockSetting { get; set; }
        public DBSetting ConfigSetting { get; set; }
        public DBSetting DescriptionSetting { get; set; }
        public DBSetting OverlaySetting { get; set; }
        public DBSetting RuleSetting { get; set; }
        public DBSetting TimeindexSetting { get; set; }
    }

    public class MongoOtherSetting
    {
        /// <summary>
        　　/// 数据库连接字符串
        　　/// </summary>
        public string IP { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Pwd { get; set; }
        public DBSetting MockSetting { get; set; }
    }
}
