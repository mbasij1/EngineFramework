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
    public class BaseKafkaConsumerTestEngine : BaseKafkaConsumerEngine
    {
        public static bool BaseFailOverTestIsRun = false;

        public BaseKafkaConsumerTestEngine(KafKaConfig Config, string topic, StorageManager storageManager) : base(Config, topic, storageManager)
        {
        }

        public override void HandleMessage(Message message)
        {
            BaseFailOverTestIsRun = true;
        }

        protected override void Work()
        {
            BaseFailOverTestIsRun = true;
        }
    }
}
