using EngineFramework.Test.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Test.DI
{
    public class Container : EngineFramework.DI.Container
    {
        public override void Config()
        {
            RegisterType<DBFailOverTestEngine>();
        }
    }
}
