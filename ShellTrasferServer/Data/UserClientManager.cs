using PostSharp.Patterns.Diagnostics;
using ShellTrasferServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class UserClientManager : IUserClientManager
    {
        public ConcurrentDictionary<string, ICallBack> StatusCallBacks { get; } = new ConcurrentDictionary<string, ICallBack>();
        public ConcurrentDictionary<string, ICallBack> CallBacks { get; } = new ConcurrentDictionary<string, ICallBack>();
        public Dictionary<string, string> NickNames { get; } = new Dictionary<string, string>();
        public HashSet<string> Deleted { get; } = new HashSet<string>();
        public HashSet<string> DeletedTasks { get; } = new HashSet<string>();
        public string SelectedClient { get; set; }
    }
}
