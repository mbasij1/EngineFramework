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
        private StorageManager _StorageManager { get; set; }

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
                var storedOffsetProcessed = GetOffsetProccessed();

                if (storedOffsetProcessed == null)
                    storedOffsetProcessed = new OffsetPosition()
                    {
                        Offset = 0,
                        PartitionId = 0
                    };

                using (var consumer = new Consumer(consumerOptions))
                {
                    var kafkaOffsets = consumer.GetTopicOffsetAsync(Topic).Result;
                    if (kafkaOffsets != null && kafkaOffsets.Count() != 0)
                    {
                        var kafkaMinOffset = kafkaOffsets.OrderBy(s => s.Offsets.Min()).FirstOrDefault();
                        var kafkaMaxOffset = kafkaOffsets.OrderByDescending(s => s.Offsets.Max()).FirstOrDefault();

                        if (storedOffsetProcessed.Offset > kafkaMaxOffset.Offsets.Max() || storedOffsetProcessed.Offset < kafkaMinOffset.Offsets.Min())
                            storedOffsetProcessed = new OffsetPosition()
                            {
                                Offset = kafkaMinOffset.Offsets.Min(),
                                PartitionId = kafkaMinOffset.PartitionId
                            };
                        else
                            storedOffsetProcessed.Offset++;

                        consumer.SetOffsetPosition(storedOffsetProcessed);
                    }

                    foreach (var message in consumer.Consume(_CancellationToken))
                    {
                        tryMesseageAgain:
                        try
                        {
                            HandleMessage(message);
                            SaveMesseageOffsetProccessed(message.Meta);
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(ex, $"Exception Occured In Kafka Consumer '{this.GetType().Name}' Offset {message.Meta.Offset} (ID={_EngineID})");
                            var delayTask = Task.Delay(5000);
                            delayTask.Wait();
                            goto tryMesseageAgain;
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {

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
