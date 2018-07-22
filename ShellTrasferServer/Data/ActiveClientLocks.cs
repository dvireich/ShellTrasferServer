using PostSharp.Patterns.Diagnostics;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellTrasferServer.Data
{
    [Log(AttributeExclude = true)]
    public class ActiveClientLocks : List<Object>, IActiveClientLocks
    {
        private readonly IMonitorHelper monitorHelper;

        public ActiveClientLocks(IMonitorHelper monitorHelper)
        {
            this.monitorHelper = monitorHelper;
        }

        public void PulseAll()
        {
            foreach(var l in this)
            {
                lock(l)
                {
                    monitorHelper.PulseAll(l);
                }
            }
        }

        void IActiveClientLocks.Remove(object lockObj)
        {
            this.Remove(lockObj);
        }

        void IActiveClientLocks.Add(object lockObj)
        {
            this.Add(lockObj);
        }
    }
}
