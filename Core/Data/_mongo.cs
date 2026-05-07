using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace FirstReg.Data
{
    public class Mongo
    {
        private readonly IMongoDatabase _db;
        private readonly string _dbname = "frdb";

        public Mongo(IMongoClient client)
        {
            _db = client.GetDatabase(_dbname);
        }

        public void InsertRecord<T>(T model, string table)
        {
            var c = _db.GetCollection<T>(table);
            c.InsertOne(model);
        }

        public List<T> Get<T>(string table)
        {
            var c = _db.GetCollection<T>(table);
            return c.Find(new BsonDocument()).ToList();
        }

        public List<T> Find<T, TField>(TField id, string table)
        {
            var c = _db.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            return c.Find(filter).ToList();
        }

        public T Get<T, TField>(TField id, string table)
        {
            var c = _db.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            return c.Find(filter).First();
        }

        public void Upsert<T, TField>(T model, TField id, string table)
        {
            var c = _db.GetCollection<T>(table);
            var filter = Builders<T>.Filter.Eq("Id", id);
            c.ReplaceOne(filter, model, new ReplaceOptions { IsUpsert = true });
        }
    }

    //public record MFilter<TField>(string key, TField Value);
}
