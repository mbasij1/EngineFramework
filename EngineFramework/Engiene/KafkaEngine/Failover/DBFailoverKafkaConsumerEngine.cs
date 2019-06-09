using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using EngineFramework.Setting;
using System.Threading.Tasks;
using System.Threading;
using EngineFramework.Engiene.KafkaEngine;
using KafkaNet.Protocol;
using KafkaNet;
using KafkaNet.Model;

namespace EngineFramework.Engiene.KafkaEngine.Failover
{
    public abstract class DBFailoverKafkaConsumerEngine : BaseKafkaConsumerEngine
    {
        private DateTime _LastRun { get; set; } = DateTime.MinValue;
        private DateTime _LastEnd { get; set; } = DateTime.MinValue;
        private object _lockLastRun { get; set; } = new object();
        private object _lockLastEnd { get; set; } = new object();

        private CancellationToken _DelayCancelation { get; set; }
        private Task _UpdateServiceRunning { get; set; }

        private DateTime _LastCheckServiceIsForThisAgent { get; set; }
        private bool _LastCheckServiceIsRunningResult { get; set; }

        public DBFailoverKafkaConsumerEngine(KafKaConfig Config, string topic) : base(Config, topic)
        {
        }

        private bool ServiceIsRunning()
        {
            if (DateTime.Now - _LastCheckServiceIsForThisAgent < new TimeSpan(0, 0, 5))
                return _LastCheckServiceIsRunningResult;

            try
            {
                string serviceName = this.GetType().FullName;
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString("AgentController")))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(@"MERGE  dbo.Services AS target
	USING  (SELECT @serviceName AS serviceName, @agentName AS agentName) AS source
	ON target.ServiceName = source.serviceName
	WHEN MATCHED AND ((target.AgentName = source.agentName AND DATEDIFF(SECOND, target.UpdateDateTime, GETDATE()) < 30) OR (DATEDIFF(SECOND, target.UpdateDateTime, GETDATE()) > 30)) THEN
		UPDATE SET AgentName =  @agentName, UpdateDateTime = GETDATE()
	WHEN NOT MATCHED THEN
	INSERT (ServiceName, AgentName, UpdateDateTime) VALUES (source.serviceName, source.agentName, GETDATE())
;

SELECT * FROM dbo.Services WHERE ServiceName = @serviceName", connection);
                    command.Parameters.AddWithValue("@agentName", _EngineID.ToString());
                    command.Parameters.AddWithValue("@serviceName", serviceName);
                    string agentName = null;
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        agentName = (string)reader["AgentName"];

                    connection.Close();

                    _LastCheckServiceIsForThisAgent = DateTime.Now;

                    if (agentName == _EngineID.ToString())
                        _LastCheckServiceIsRunningResult = true;
                    else
                        _LastCheckServiceIsRunningResult = false;

                    return _LastCheckServiceIsRunningResult;
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Can't Check That Service On This Node Should Run Or Not");
                return false;
            }
        }

        public override void Start()
        {
            base.Start();
            var tokenSource = new CancellationTokenSource();
            _DelayCancelation = tokenSource.Token;
            _UpdateServiceRunning = Task.Factory.StartNew(UpdateServiceIsRunning, _DelayCancelation);
        }

        private void UpdateServiceIsRunning()
        {
            var updateInterVal = new TimeSpan(0, 0, 5);
            while (!_DelayCancelation.IsCancellationRequested)
            {
                ServiceIsRunning();
                var temp = Task.Delay(updateInterVal);
                temp.Wait();
            }
        }

        protected override void EngineController()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started.");
            _LastCheckServiceIsForThisAgent = DateTime.Now.AddSeconds(-30);
            while (!_CancellationToken.IsCancellationRequested)
            {
                if (!ServiceIsRunning())
                {
                    var delayTask = Task.Delay(new TimeSpan(0, 0, 5), _DelayCancelation);
                    delayTask.Wait();
                    continue;
                }

                var kafkaOptions = new KafkaOptions(new Uri(_Config.URL));
                var BrokerRouter = new BrokerRouter(kafkaOptions);

                var consumerOptions = new ConsumerOptions(Topic, BrokerRouter);
                //consumerOptions.MaxWaitTimeForMinimumBytes = new TimeSpan(0, 0, 5);
                //consumerOptions.MinimumBytes = 2;
                //consumerOptions.FetchBufferMultiplier = 1;
                //consumerOptions.TopicPartitionQueryTimeMs = 100;
                var offsetProcessed = GetOffsetProccessed();
                using (var consumer = offsetProcessed == null ?
                    new Consumer(consumerOptions) :
                    new Consumer(consumerOptions, new OffsetPosition(offsetProcessed.PartitionId, offsetProcessed.Offset + 1)))
                {
                    foreach (var message in consumer.Consume(_CancellationToken))
                    {
                        try
                        {
                            if (!ServiceIsRunning())
                                break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");
                        }

                        lock (_lockLastRun)
                        {
                            _LastRun = DateTime.Now;
                        }

                        try
                        {
                            HandleMessage(message);

                            SaveMesseageOffsetProccessed(message.Meta);
                        }
                        catch (Exception ex)
                        {
                            logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");

                            consumer.SetOffsetPosition(new OffsetPosition(message.Meta.PartitionId, message.Meta.Offset - 1));
                        }

                        lock (_lockLastEnd)
                        {
                            _LastEnd = DateTime.Now;
                        }
                    }
                }
            }

            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }
    }
}
