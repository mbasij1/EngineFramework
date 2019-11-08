using EngineFramework.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EngineFramework.Engiene
{
    public abstract class BaseEngine
    {
        protected ILogger logger;

        private Task _Task { get; set; }

        public bool IsStoped { get { return _Task?.IsCompleted ?? true; } }

        private CancellationTokenSource _TokenSource { get; set; }

        protected CancellationToken _CancellationToken { get; set; }

        protected Guid _EngineID;

        protected TimeSpan IntervalWorkCall { get; set; }

        public BaseEngine()
        {
            IntervalWorkCall = new TimeSpan(1000000);
            logger = EngineFrameworkLoggerFactory.CreateLogger(this.GetType());
            _EngineID = Guid.NewGuid();
        }

        public virtual void Start()
        {
            if (!IsStoped)
            {
                logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started Before.");
                return;
            }

            _TokenSource = new CancellationTokenSource();
            _CancellationToken = _TokenSource.Token;
            _Task = Task.Factory.StartNew(EngineController, _CancellationToken);
        }

        protected virtual void EngineController()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started.");

            while (!_CancellationToken.IsCancellationRequested)
            {
                try
                {
                    Work();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");
                }

                if (IntervalWorkCall.Ticks != 0)
                {
                    var temp = Task.Delay(IntervalWorkCall);
                    temp.Wait();
                }
            }

            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }

        public virtual void Stop()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Closing Started.");
            _TokenSource.Cancel();
            OnStop();
            while (_Task != null && !_Task.IsCompleted)
            {
                var temp = Task.Delay(100);
                temp.Wait();
            }
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Closed.");
        }

        protected virtual void OnStop()
        {

        }

        protected abstract void Work();
    }
}
