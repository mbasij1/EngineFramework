using EngineFramework.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EngineFramework.Engiene
{
    public class EnginesManager
    {
        protected ILogger logger = EngineFrameworkLoggerFactory.CreateLogger(typeof(EnginesManager));

        private List<BaseEngine> _Engines;

        public EnginesManager()
        {
            _Engines = new List<BaseEngine>();
        }

        public void AddEngine(BaseEngine engine) => _Engines.Add(engine);

        public void Start()
        {
            logger.LogInformation("Start Engines ...");
            foreach (var engine in _Engines)
            {
                try
                {
                    engine.Start();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Can't Start {engine.GetType().Name}");
                }
            }

            logger.LogInformation("Start Engines Complete.");
        }

        public void Stop()
        {
            logger.LogInformation("Start Stoping Engine.");

            Parallel.ForEach(_Engines,engine => engine.Stop());

            bool isAllStoped = true;
            do
            {
                for (int i = 0; i < _Engines.Count; i++)
                    isAllStoped = _Engines[i].IsStoped && isAllStoped;
            } while (!isAllStoped);
            logger.LogInformation("Engines Stoped.");
        }
    }
}
