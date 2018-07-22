using ShellTrasferServer;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Data
{
    public interface IUserClientManager
    {
        ConcurrentDictionary<string, ICallBack> StatusCallBacks { get; }
        ConcurrentDictionary<string, ICallBack> CallBacks { get; }
        Dictionary<string, string> NickNames { get; }
        HashSet<string> Deleted { get; }
        HashSet<string> DeletedTasks { get; }
        string SelectedClient { get; set; }
    }
}
