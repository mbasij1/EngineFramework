using EngineFramework.Engiene.FailOver;
using EngineFramework.Engiene.KafkaEngine;
using EngineFramework.Engiene.KafkaEngine.Failover;
using EngineFramework.Storages;
using KafkaNet.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Test.Engine
{
    public class DBFailoverKafkaConsumerTestEngine : DBFailoverKafkaConsumerEngine
    {
        public static bool DBFailOverTestIsRun = false;

        public DBFailoverKafkaConsumerTestEngine(KafKaConfig Config, string topic, StorageManager storageManager) : base(Config, topic, storageManager)
        {
        }

        public override void HandleMessage(Message message)
        {
            DBFailOverTestIsRun = true;
        }

        protected override void Work()
        {
            DBFailOverTestIsRun = true;
        }
    }
}
