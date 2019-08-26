using EngineFramework.Context;
using MongoDB.Driver;

namespace EngineFramework.Storages
{
    public class MongoStorageManager : StorageManager
    {
        private MongoDbContext _MongoDbContext { get; set; }

        public MongoStorageManager(MongoDbContext MongoDbContext)
        {
            _MongoDbContext = MongoDbContext;
        }

        public T GetSetting<T>(string schema, string key)
        {
            var tt = _MongoDbContext.Storages<T>().Find(s => s.Schema == schema && s.Key == key).SingleOrDefault();
            if (tt == null)
                return default(T);

            return tt.Value;
        }

        public void SaveSetting<T>(string schema, string key, T value)
        {
            var temp = _MongoDbContext.Storages<T>().Find(s => s.Schema == schema && s.Key == key).SingleOrDefault();

            if (temp == null)
            {
                temp = new Storage<T>();
                temp.Schema = schema;
                temp.Key = key;
                temp.Value = value;
                _MongoDbContext.Storages<T>().InsertOne(temp);

            }
            else
            {
                var filter = Builders<Storage<T>>.Filter.Eq(s => s.Schema, schema);
                var up = Builders<Storage<T>>.Update.Set(s => s.Value, value);
                _MongoDbContext.Storages<T>().UpdateOne(s => s.Schema == schema && s.Key == key, up);
            }
        }
    }
}
