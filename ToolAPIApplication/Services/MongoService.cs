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
        private MongoSetting _config;
        private MongoClient _client = null;

        public MongoService(IOptions<MongoSetting> setting)
        {
            // 自己的库
            _config = setting.Value;
            string conn = "mongodb://" + _config.IP + ":" + _config.Port;
            _client = new MongoClient(conn);

            
        }

        public RuleBo QueryRule(string name)
        {
            var collection = _client.GetDatabase(_config.RuleSetting.Database)
                                   .GetCollection<BsonDocument>(_config.RuleSetting.Collection);
            var list = collection.Find(Builders<BsonDocument>.Filter.Eq("name", name)).ToList();
            foreach (var doc in list)
            {
                var bo = BsonSerializer.Deserialize<RuleBo>(doc);
                return bo;
            }
            return null;
        }
    }
}
