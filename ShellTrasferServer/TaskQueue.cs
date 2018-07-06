using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class TaskQueue
    {
        private static volatile TaskQueue instance;
        private static object syncRoot = new Object();

        private TaskQueue() { }

        private ConcurrentDictionary<string, UserTaskQueue> _userToUserTaskQueue = new ConcurrentDictionary<string, UserTaskQueue>();

        public UserTaskQueue CurrentUserTaskQueue
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserTaskQueue.ContainsKey(activeUserId))
                {
                    _userToUserTaskQueue[activeUserId] = new UserTaskQueue();
                }
                return _userToUserTaskQueue[activeUserId];
            }
        }

        public static TaskQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TaskQueue();
                    }
                }

                return instance;
            }
        }

    }
}
