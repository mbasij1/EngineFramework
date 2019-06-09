using EngineFramework.DI;
using EngineFramework.Engiene;
using EngineFramework.Logging;
using EngineFramework.Setting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EngineFramework.Agent
{
    public static class AgentController
    {
        public static ILogger logger;

        public static bool IsConsoleApplication { get; set; }
        public static bool IsProvidersInitiate { get; set; }
        public static bool IsService { get; set; }

        public static Task providerTask;

        public static Action runProvider,
               shutdownProvider;

        private static EnginesManager _enginesManager;

        public static void init(Container container)
        {
            logger = EngineFrameworkLoggerFactory.CreateLogger(typeof(AgentController));
            logger.LogInformation("Logger Config Set.");

            container.Config();
            logger.LogInformation("DI Config Set.");

            _enginesManager = new EnginesManager();

            foreach (var engine in AppSettings.EngineNames)
            {
                var temp = Container.Resolve<BaseEngine>(engine);
                if (temp != null)
                    _enginesManager.AddEngine(temp);
            }

            //_enginesManager.AddEngine(Container.Resolve<StatementEngine>());
            logger.LogInformation("Engines Add.");
        }

        public static void initWorkers()
        {
            //Create Actions
            runProvider = delegate
            {
                System.Threading.Thread.CurrentThread.IsBackground = true;
                _enginesManager.Start();
                logger.LogInformation("Engine Started.");
                Control("/?");
            };

            shutdownProvider = delegate
            {
                _enginesManager.Stop();
                logger.LogInformation("Engine Stoped.");
            };

        }

        public static void ExexuteTasks()
        {
            //Execute Tasks
            ExecuteProviderTask();
        }

        public static void ExecuteProviderTask()
        {
            if (!IsProvidersInitiate)
            {
                IsProvidersInitiate = true;
                providerTask = new Task(runProvider);
                providerTask.ContinueWith(ExceptionProvidersHandler, TaskContinuationOptions.LongRunning);
                providerTask.Start();
                IsProvidersInitiate = false;
            }
        }


        private static void ExceptionProvidersHandler(Task task)
        {
            var ex = task.Exception;
            if (ex != null)
            {
                logger.LogError(ex, "Failure Exception On Providers");
            }
        }

        public static void RestartTasks()
        {
            logger.LogWarning("Force Restart Systems ...");
            shutdownProvider.Invoke();
            providerTask.Dispose();
            initWorkers();
            ExexuteTasks();
        }

        public static void RestartProviders()
        {
            if (!IsProvidersInitiate)
            {
                IsProvidersInitiate = true;
                logger.LogWarning("Force Restart Providers ...");
                shutdownProvider.Invoke();
                providerTask.Dispose();
                ExecuteProviderTask();
                IsProvidersInitiate = false;
            }
        }

        public static bool Control(string key)
        {
            bool result = false;

            if (key == null)
                return result;

            string[] splitedCommand = key.Split(' ');

            switch (key.ToLower().Split(' ')[0])
            {
                case "/?":
                    logger.LogInformation("Help for commands : \n{\n "
                                               + "        /? ==> Help \n"
                                               + "        /shutdown ==> Shutdown all services and kill all task \n"
                                               + "        /restart ==> Restart All Services \n"
                                               + "        /restartProviders ==> Restart All Providers \n"
                                               + "        /start ==> Start All Services \n"
                                               + "        /startProviders ==> Start All providers \n"
                                               + "}");
                    break;
                case "/shutdown":
                    try
                    {
                        //Shutdown System
                        logger.LogWarning("Force ShutDown System ...");
                        shutdownProvider.Invoke();

                        providerTask.Dispose();
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "Force ShutDown System Failed");
                    }
                    break;
                case "/restart":
                    try
                    {
                        RestartTasks();
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "Force ShutDown System Failed");
                    }
                    break;
                case "/restartproviders":
                    try
                    {
                        RestartProviders();
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "Force ShutDown System Failed");
                    }
                    break;


                case "/start":
                    providerTask.Dispose();
                    ExexuteTasks();
                    break;
                case "/startproviders":
                    providerTask.Dispose();
                    ExecuteProviderTask();
                    break;
                default:
                    logger.LogError("Bad Command Entered by User");
                    break;
            }
            return result;
        }
    }
}
