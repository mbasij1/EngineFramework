using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using EngineFramework.Setting;
using System.Threading.Tasks;
using System.Threading;

namespace EngineFramework.Engiene.FailOver
{
    public abstract class DBFailOverEngine : BaseEngine
    {
        private DateTime _LastRun { get; set; } = DateTime.MinValue;
        private DateTime _LastEnd { get; set; } = DateTime.MinValue;
        private object _lockLastRun { get; set; } = new object();
        private object _lockLastEnd { get; set; } = new object();

        private CancellationToken _DelayCancelation { get; set; }
        private Task _UpdateServiceRunning { get; set; }
        private DateTime _LastCheckServiceIsForThisAgent { get; set; }
        private bool _LastCheckServiceIsRunningResult { get; set; }

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
                        _LastCheckServiceIsRunningResult =  false;

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
                lock (_lockLastRun)
                {
                    _LastRun = DateTime.Now;
                }

                try
                {
                    if (ServiceIsRunning())
                        Work();
                    else if(IntervalWorkCall.Ticks == 0)
                    {
                        var temp = Task.Delay(new TimeSpan(0, 0, 5), _DelayCancelation);
                        temp.Wait();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");
                }

                lock (_lockLastEnd)
                {
                    _LastEnd = DateTime.Now;
                }

                if (IntervalWorkCall.Ticks != 0)
                {
                    var temp = Task.Delay(IntervalWorkCall, _DelayCancelation);
                    temp.Wait();
                }
            }

            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }
    }
}
