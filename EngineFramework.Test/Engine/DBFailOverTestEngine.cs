using EngineFramework.Engiene.FailOver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Test.Engine
{
    public class DBFailOverTestEngine : DBFailOverEngine
    {
        public static bool DBFailOverTestIsRun = false;

        protected override void Work()
        {
            DBFailOverTestIsRun = true;
        }
    }
}
