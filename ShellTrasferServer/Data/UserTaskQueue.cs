using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class UserTaskQueue : IUserTaskQueue
    {
        public ConcurrentDictionary<string, Queue<ShellTask>> ShellQueue { get; } = new ConcurrentDictionary<string, Queue<ShellTask>>();
        public ConcurrentDictionary<string, Queue<TransferTask>> TransferTaskQueue { get; } = new ConcurrentDictionary<string, Queue<TransferTask>>();
    }
}
