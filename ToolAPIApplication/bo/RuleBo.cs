using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToolAPIApplication.bo
{
    public class RuleBO
    {
        public ObjectId _id { get; set; }
        public string name { get; set; }
        public string unit { get; set; }
        public double limits { get; set; }
        public string description { get; set; }
    }
}
