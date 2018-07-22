using ShellTrasferServer.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Data
{
    public class ShellTaskLockManager
    {
        private static object syncRoot = new Object();
        private static volatile ShellTaskLockManager instance;
        private ShellTaskLockManager() { }

        public static ShellTaskLockManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new ShellTaskLockManager();
                    }
                }

                return instance;
            }
        }

        private ConcurrentDictionary<string, ActiveClientLocks> _userToLockObject = new ConcurrentDictionary<string, ActiveClientLocks>();

        public ActiveClientLocks CurrentUserLockMannager
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToLockObject.ContainsKey(activeUserId))
                    _userToLockObject[activeUserId] = new ActiveClientLocks(new MonitorHelper());
                return _userToLockObject[activeUserId];
            }
        }
    }
}
