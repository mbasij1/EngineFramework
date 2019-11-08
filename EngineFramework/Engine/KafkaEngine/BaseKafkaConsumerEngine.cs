using EngineFramework.Setting;
using EngineFramework.Storages;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineFramework.Engiene.KafkaEngine
{
    public abstract class BaseKafkaConsumerEngine : BaseEngine
    {
        public KafKaConfig _Config { get; set; }
        public string Topic { get; set; }
        private StorageManager _StorageManager { get; set;}

        public BaseKafkaConsumerEngine(KafKaConfig Config, string topic, StorageManager storageManager)
        {
            _Config = Config;
            Topic = topic;
            _StorageManager = storageManager;
        }

        protected override void EngineController()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started.");

        tryRecover:
            try
            {
                var kafkaOptions = new KafkaOptions(new Uri(_Config.URL));
                var BrokerRouter = new BrokerRouter(kafkaOptions);

                var consumerOptions = new ConsumerOptions(Topic, BrokerRouter);
                //consumerOptions.MaxWaitTimeForMinimumBytes = new TimeSpan(0, 0, 5);
                //consumerOptions.MinimumBytes = 2;
                //consumerOptions.FetchBufferMultiplier = 1;
                //consumerOptions.TopicPartitionQueryTimeMs = 100;
                int i = 0;
                var offsetProcessed = GetOffsetProccessed();

                if (offsetProcessed == null)
                    offsetProcessed = new OffsetPosition()
                    {
                        Offset = 0,
                        PartitionId = 0
                    };

                using (var consumer = new Consumer(consumerOptions))
                {
                    var kafkaOffsets = consumer.GetOffsetPosition();
                    if (kafkaOffsets != null)
                    {
                        var kafkaMinOffset = kafkaOffsets.OrderByDescending(s => s.Offset).FirstOrDefault();
                        if (kafkaMinOffset != null && offsetProcessed.Offset < kafkaMinOffset.Offset)
                        {
                            offsetProcessed = kafkaMinOffset;
                        }
                    }

                    consumer.SetOffsetPosition(offsetProcessed);
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
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Exception Occured In Kafka Consumer '{this.GetType().Name}' (ID={_EngineID}) {ex.Message} \n {ex.StackTrace}.");
                logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Recovering...");
                goto tryRecover;
            }
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }

        protected OffsetPosition GetOffsetProccessed()
        {
            return _StorageManager.GetSetting<OffsetPosition>(this.GetType().FullName, Topic);
        }

        protected void SaveMesseageOffsetProccessed(MessageMetadata messageMetadata)
        {
            _StorageManager.SaveSetting(this.GetType().FullName, Topic, messageMetadata);
        }

        public abstract void HandleMessage(Message message);
    }
}
