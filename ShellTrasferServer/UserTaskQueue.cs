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
        private ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>> _shellTaskQueue = new ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>>();
        private ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>> _transferTaskQueue =
            new ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>>();
        public ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>> ShellQueue { get { return _shellTaskQueue; } }
        public ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>> TransferTaskQueue { get { return _transferTaskQueue; } }
    }
}
