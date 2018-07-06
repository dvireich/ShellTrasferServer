using PostSharp.Extensibility;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserLoader
{
    [Log(AttributeTargetElements= MulticastTargets.Method, AttributeTargetTypeAttributes= MulticastAttributes.Public, AttributeTargetMemberAttributes= MulticastAttributes.Public)]
    public class TaskQueue
    {
        private static volatile TaskQueue instance;
        private static object syncRoot = new Object();

        private ConcurrentQueue<string> taskQueue = new ConcurrentQueue<string>();

        public void AddToTaskQueue(string id)
        {
            taskQueue.Enqueue(id);
        }

        public string GetNextTask()
        {
            taskQueue.TryDequeue(out string task);
            return task;
        }

        [Log(AttributeExclude = true)]
        public bool Any()
        {
            return taskQueue.Count > 0;
        }

        [Log(AttributeExclude = true)]
        private TaskQueue() { }

        [Log(AttributeExclude = true)]
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
