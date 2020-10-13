using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToolAPIApplication.bo;

namespace ToolAPIApplication.Services
{
    public class MongoService : IMongoService
    {
        public IConfiguration Configuration { get; }

        private MongoClient _client = null;

        public MongoService(IConfiguration configuration)
        {
            Configuration = configuration;

            // 自己的库
            string conn = "mongodb://" + Configuration["MongoSetting:Ip"] + ":" + Configuration["MongoSetting:Port"];
            _client = new MongoClient(conn);


        }

        public RuleBO QueryRule(string name)
        {
            var collection = _client.GetDatabase(Configuration["MongoSetting:RuleSetting:Database"])
                                   .GetCollection<BsonDocument>(Configuration["MongoSetting:RuleSetting:Collection"]);
            var list = collection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).ToList();
            foreach (var doc in list)
            {
                var bo = BsonSerializer.Deserialize<RuleBO>(doc);
                return bo;
            }
            return null;
        }
    }
}
