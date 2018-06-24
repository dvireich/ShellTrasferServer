using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    public class UserTaskQueue
    {
        private ConcurrentDictionary<string, Queue<ShellTask>> _shellTaskQueue = new ConcurrentDictionary<string, Queue<ShellTask>>();
        public ConcurrentDictionary<string, Queue<ShellTask>> ShellQueue { get { return _shellTaskQueue; } }


        private ConcurrentDictionary<string, Queue<TransferTask>> _transferTaskQueue =  new ConcurrentDictionary<string, Queue<TransferTask>>();
        public ConcurrentDictionary<string, Queue<TransferTask>> TransferTaskQueue { get { return _transferTaskQueue; } }
    }
}
