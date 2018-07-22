using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class MonitorHelper : IMonitorHelper
    {
        public void PulseAll(object lockObj)
        {
            Monitor.PulseAll(lockObj);
        }

        public bool Wait(object lockObj)
        {
            return Monitor.Wait(lockObj);
        }
    }
}
