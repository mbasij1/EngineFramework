using EngineFramework.Setting;
using EngineFramework.Storage;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace EngineFramework.Engiene.KafkaEngine
{
    public abstract class BaseKafkaConsumerEngine : BaseEngine
    {
        public KafKaConfig _Config { get; set; }
        public string Topic { get; set; }
        public BaseKafkaConsumerEngine(KafKaConfig Config, string topic)
        {
            _Config = Config;
            Topic = topic;
        }

        protected override void EngineController()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started.");

            var kafkaOptions = new KafkaOptions(new Uri(_Config.URL));
            var BrokerRouter = new BrokerRouter(kafkaOptions);

            var consumerOptions = new ConsumerOptions(Topic, BrokerRouter);
            //consumerOptions.MaxWaitTimeForMinimumBytes = new TimeSpan(0, 0, 5);
            //consumerOptions.MinimumBytes = 2;
            //consumerOptions.FetchBufferMultiplier = 1;
            //consumerOptions.TopicPartitionQueryTimeMs = 100;
            int i = 0;
            var offsetProcessed = GetOffsetProccessed();
            using (var consumer = new Consumer(consumerOptions, new OffsetPosition(offsetProcessed.PartitionId, offsetProcessed.Offset + 1)))
            {
                foreach (var message in consumer.Consume(_CancellationToken))
                {
                    try
                    {
                        HandleMessage(message);

                        i++;
                        if (i == 100)
                        {
                            SaveMesseageOffsetProccessed(message.Meta);
                            i = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");

                        consumer.SetOffsetPosition(new OffsetPosition(message.Meta.PartitionId, message.Meta.Offset - 1));
                    }
                }
            }

            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }

        protected OffsetPosition GetOffsetProccessed()
        {
            return StorageManager.GetSetting<OffsetPosition>(this.GetType().FullName, "TopicName");
        }

        protected void SaveMesseageOffsetProccessed(MessageMetadata messageMetadata)
        {
            StorageManager.SaveSetting(this.GetType().FullName, "TopicName", messageMetadata);
        }

        public abstract void HandleMessage(Message message);
    }
}
