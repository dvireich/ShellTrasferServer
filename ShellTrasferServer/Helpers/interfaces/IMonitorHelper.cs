using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IMonitorHelper
    {
        void PulseAll(Object lockObj);

        bool Wait(Object lockObj);
    }
}
