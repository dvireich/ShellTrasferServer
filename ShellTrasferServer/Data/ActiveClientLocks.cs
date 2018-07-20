using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellTrasferServer.Data
{
    [Log(AttributeExclude = true)]
    public class ActiveClientLocks : List<Object>
    {
        public void PulseAll()
        {
            foreach(var l in this)
            {
                lock(l)
                {
                    Monitor.PulseAll(l);
                }
            }
        }
    }
}
