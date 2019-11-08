using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EngineFramework.Engiene.FailOver
{
    public abstract class SocketFailOverEngine : BaseEngine
    {
        private static IEnumerable<Node> FailOverNodes { get; set; }
        //private static Dictionary<string, bool> RunningNodeStatus { get; set; }
        private static object _LockRunningNode { get; set; }
        private static IPHostEntry _IPHostEntry { get; set; }
        private static IPAddress _IPAddress { get; set; }
        private static IPEndPoint _IPEndPoint { get; set; }
        private static Socket _Socket { get; set; }
        private static Task FailOverResponser { get; set; }
        private static Task NodeStatusChecker { get; set; }

        private static List<string> ServicesCreated { set; get; }
        private static object _LockServicesCreated { get; set; }
        public SocketFailOverEngine()
        {
            lock (_LockServicesCreated)
            {
                string serviceName = this.GetType().Name;
                if (ServicesCreated.Contains(serviceName))
                    throw new Exception($"Can't Run Multiple Instance From FailOverEngines {serviceName}.");

                ServicesCreated.Add(serviceName);
            }
        }

        private static DateTime _LastCheckStatus { get; set; }
        static SocketFailOverEngine()
        {
            ServicesCreated = new List<string>();
            _LastCheckStatus = DateTime.MinValue;
            _IPAddress = IPAddress.Any;// _IPHostEntry.AddressList[0];
            _IPEndPoint = new IPEndPoint(_IPAddress, 11000);
            _Socket = new Socket(_IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _Socket.Bind(_IPEndPoint);
                _Socket.Listen(30);
                //_Socket.ReceiveTimeout = 10;

                FailOverResponser = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        byte[] bytes = new Byte[1024];
                        Socket handler;
                        try
                        {
                            handler = _Socket.Accept();
                        }
                        catch (Exception e)
                        {
                            continue;
                        }

                        try
                        {
                            int bytesRec = handler.Receive(bytes);
                            byte[] serviceNamebytes = new Byte[1023];
                            byte code = bytes[0];
                            bytes.CopyTo(serviceNamebytes, 1);
                            string serviceName = Encoding.ASCII.GetString(bytes);
                            byte[] msg = new byte[1];
                            // msg 100: Command Not Exist
                            // msg 101: Internal Server Error
                            // msg 0 => StandBy
                            // msg 1 => Running
                            // msg 2 => This Service Not Exist In This Node
                            switch (code)
                            {
                                // Check Status
                                case 0:
                                    if (ServiceIsRunning(serviceName))
                                        msg[0] = 1;
                                    else
                                        msg[0] = 0;
                                    break;

                                default:
                                    msg[0] = 100;
                                    break;
                            }

                            handler.Send(msg);
                        }
                        catch (Exception)
                        {
                            var msg = new byte[1];
                            msg[0] = 100;
                            handler.Send(msg);
                        }
                        finally
                        {
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                        }
                    }
                });

                NodeStatusChecker = Task.Factory.StartNew(() =>
                {
                    do
                    {
                        if (_LastCheckStatus == DateTime.MinValue || _LastCheckStatus < _LastCheckStatus.AddSeconds(20))
                        {
                            lock (_LockServicesCreated)
                            {
                                foreach (var service in ServicesCreated)
                                {
                                    //var IsServiceRunning = ServiceIsRunning(service);
                                    var temp = FailOverNodes.Where(s => s.ServiceName == service).OrderBy(s => s.Priority).ToArray();
                                    var localNode = temp.Where(s => IsLocalService(s)).SingleOrDefault();

                                    if (localNode == null)
                                        continue;

                                    var failOverCount = temp.Count();

                                    for (int i = 0; i < failOverCount; i++)
                                    {
                                        byte status = CheckServiceStatus(temp[i]);

                                        switch (status)
                                        {
                                            case 0: //StandBy
                                                if (temp[i].Status != NodeStatus.StandBy)
                                                    lock (_LockRunningNode)
                                                    {
                                                        temp[i].Status = NodeStatus.StandBy;
                                                        temp[i].LastUpdateStatus = DateTime.Now;
                                                    }
                                                break;

                                            case 1: //Running
                                                if (temp[i].Status != NodeStatus.Running)
                                                    lock (_LockRunningNode)
                                                    {
                                                        temp[i].Status = NodeStatus.Running;
                                                        temp[i].LastUpdateStatus = DateTime.Now;
                                                    }
                                                break;

                                            case 2://This Service Not Exist In This Node
                                            case 102://Not Response Recive Or Faild Check Status
                                                if (temp[i].Status != NodeStatus.Stop)
                                                    lock (_LockRunningNode)
                                                    {
                                                        temp[i].Status = NodeStatus.Stop;
                                                        temp[i].LastUpdateStatus = DateTime.Now;
                                                    }
                                                break;

                                            case 100://Command Not Exist
                                            case 101://Internal Server Error
                                            default:
                                                if (temp[i].Status != NodeStatus.Unknown)
                                                    lock (_LockRunningNode)
                                                    {
                                                        temp[i].Status = NodeStatus.Unknown;
                                                        temp[i].LastUpdateStatus = DateTime.Now;
                                                    }
                                                if (temp[i].LastUpdateStatus.AddSeconds(60) < DateTime.Now)
                                                    lock (_LockRunningNode)
                                                    {
                                                        temp[i].Status = NodeStatus.Stop;
                                                        temp[i].LastUpdateStatus = DateTime.Now;
                                                    }
                                                break;
                                        }
                                    }

                                    bool isShouldRun = false;
                                    bool isAllNodesWithUperPriorityStoped = false;
                                    for (int i = 0; i < failOverCount; i++)
                                    {
                                        if (temp[i].Status == NodeStatus.Running)
                                        {
                                            if (IsLocalService(temp[i]))
                                                isShouldRun = true;
                                            else
                                                isShouldRun = false;

                                            break;
                                        }
                                        else if (IsLocalService(temp[i]))
                                        {
                                            if (isAllNodesWithUperPriorityStoped)
                                                isShouldRun = true;
                                            else
                                                isShouldRun = false;
                                        }
                                        else if (temp[i].Status == NodeStatus.Stop)
                                        {
                                            isAllNodesWithUperPriorityStoped = true;
                                        }
                                        else if (temp[i].Status == NodeStatus.StandBy || temp[i].Status == NodeStatus.Unknown)
                                        {
                                            isAllNodesWithUperPriorityStoped = false;
                                        }
                                    }

                                    var newNodeStatus = isShouldRun ? NodeStatus.Running : NodeStatus.StandBy;
                                    if (localNode.Status != newNodeStatus)
                                        lock (_LockRunningNode)
                                        {
                                            localNode.Status = newNodeStatus;
                                        }
                                }
                                _LastCheckStatus = DateTime.Now;
                            }
                        }
                        Task.Delay(100).Wait();
                    } while (true);
                });
            }
            catch (Exception e)
            {
                throw new Exception($"Faild Launch FailOverEngine On { _IPEndPoint }", e);
            }
        }

        private static bool IsLocalService(Node node)
        {
            return string.IsNullOrWhiteSpace(node.Address) || node.Address == "*";
        }

        /// <summary>
        /// msg 100: Command Not Exist
        /// msg 101: Internal Server Error
        /// msg 102: Not Response Recive Or Faild Check Status
        /// msg 0 => StandBy
        /// msg 1 => Running
        /// msg 2 => This Service Not Exist In This Node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static byte CheckServiceStatus(Node node)
        {
            throw new NotImplementedException();
        }

        private bool ServiceIsRunning()
        {
            return ServiceIsRunning(this.GetType().Name);
        }

        private static bool ServiceIsRunning(string serviceName)
        {
            lock (_LockRunningNode)
            {
                var temp = FailOverNodes.Where(s => s.ServiceName == serviceName && (string.IsNullOrWhiteSpace(s.Address) || s.Address == "*")).SingleOrDefault();

                if (temp == null)
                    return false;

                if (temp.Status == NodeStatus.Running)
                    return true;

                //return FailOverNodes.Where(s => s.ServiceName == serviceName && (string.IsNullOrWhiteSpace(s.Address) || s.Address == "*")).SingleOrDefault()?.Status == NodeStatus.Running ? true : false;
            }
            return false;
        }

        protected override void EngineController()
        {
            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Started.");
            while (!_CancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!ServiceIsRunning())
                        Work();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"Exception Occured In Engine Work, (ID={_EngineID})");
                }

                var temp = Task.Delay(100);
                temp.Wait();
            }

            logger.LogInformation($"'{this.GetType().Name}' (ID={_EngineID}) Stoped.");
        }
    }
}
