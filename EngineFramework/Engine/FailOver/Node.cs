using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Engiene.FailOver
{
    public class Node
    {
        public string ServiceName { get; set; }

        public string Address { get; set; }

        public NodeStatus Status { get; set; }

        public byte Priority { get; set; }
        public DateTime LastUpdateStatus { get; set; }
    }
}
