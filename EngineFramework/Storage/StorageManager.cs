using EngineFramework.Context;
using KafkaNet.Protocol;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Storages
{
    public interface StorageManager
    {
        T GetSetting<T>(string schema, string key);

        void SaveSetting<T>(string schema, string key, T value);
    }
}
