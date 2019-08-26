using EngineFramework.Agent;
using EngineFramework.Test.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace EngineFramework.Test
{
    [TestClass]
    public class AgentControllerTest
    {
        [TestMethod, Timeout(120000)]
        public void TestKafkaConsumerDBFailOverEngine()
        {
            DBFailoverKafkaConsumerTestEngine d = new DBFailoverKafkaConsumerTestEngine(new Engiene.KafkaEngine.KafKaConfig() { URL = "http://192.168.87.11:9092" }, "test", new Storages.SqlStorageManager());
            d.Start();
            Task.Delay(100).Wait();

            d.Stop();
            while (true)
            {
                Task.Delay(100).Wait();
                if (d.IsStoped)
                    break;
            }
        }

        [TestMethod, Timeout(40000)]
        public void TestDBFailOverEngine()
        {
            DBFailOverTestEngine d = new DBFailOverTestEngine();
            d.Start();
            while (true)
            {
                Task.Delay(100).Wait();
                if (DBFailOverTestEngine.DBFailOverTestIsRun == true)
                    break;
            }
            d.Stop();
        }

        //[TestMethod,Timeout(10000)]
        //public void TestDBFailOverEngine()
        //{
        //    AgentController.init(new DI.Container());
        //    AgentController.initWorkers();
        //    AgentController.ExexuteTasks();

        //    while (true)
        //    {
        //        Task.Delay(10000).Wait();
        //        if (DBFailOverTestEngine.DBFailOverTestIsRun == true)
        //            break;
        //    }
        //}
    }
}
