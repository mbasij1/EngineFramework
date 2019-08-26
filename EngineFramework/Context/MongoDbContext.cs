using EngineFramework.Storages;
using MongoDB.Driver;

namespace EngineFramework.Context
{
    public class MongoDbContext
    {
        private string _ConnectionString { get; set; }

        private string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }
            set
            {
                _ConnectionString = value;
            }
        }

        private string _DatabaseName { get; set; }

        private string DatabaseName
        {
            get
            {
                return _DatabaseName;
            }

            set
            {
                _DatabaseName = value;
            }
        }

        private MongoClient _MongoClient { get; set; }

        private IMongoDatabase _MongoDatabase { get; set; }

        public MongoDbContext(MongoDBConnectionString mongoDBConnectionString)
        {
            ConnectionString = mongoDBConnectionString.ConnectionString;
            DatabaseName = mongoDBConnectionString.Database;

            _MongoClient = new MongoClient(ConnectionString);
            _MongoDatabase = _MongoClient.GetDatabase(DatabaseName);
        }

        public IMongoCollection<MongoDB.Bson.BsonDocument> GetCollection(string collentionName) => _MongoDatabase.GetCollection<MongoDB.Bson.BsonDocument>(collentionName);
        public IMongoCollection<T> GetCollection<T>(string collentionName) => _MongoDatabase.GetCollection<T>(collentionName);
        public IMongoCollection<Storage<T>> Storages<T>() => _MongoDatabase.GetCollection<Storage<T>>("Storages");
    }
}
