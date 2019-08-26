using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Storages
{
    public class Storage<T>
    {
        public MongoDB.Bson.ObjectId _id { get; set; }
        public string Key { get; set; }
        public T Value { get; set; }
        public string Schema { get; set; }
    }
}
