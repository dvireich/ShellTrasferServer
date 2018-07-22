using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Data
{
    public interface IUserTaskQueue
    {
        ConcurrentDictionary<string, Queue<ShellTask>> ShellQueue { get; }
        ConcurrentDictionary<string, Queue<TransferTask>> TransferTaskQueue { get; }
    }
}
