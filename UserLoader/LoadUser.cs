using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using WcfLogger;

[assembly: Log]
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace UserLoader
{
    [LoggingBehavior]
    public class LoadUser : ILoadUser
    {
        bool ILoadUser.LoadUser(string id)
        {
            TaskQueue.Instance.AddToTaskQueue(id);
            return true;
        }
    }

    public class TaskQueue
    {
        private static volatile TaskQueue instance;
        private static object syncRoot = new Object();

        private Queue<string> taskQueue = new Queue<string>();

        public void AddToTaskQueue(string id)
        {
            taskQueue.Enqueue(id);
        }

        public string GetNextTask()
        {
            return taskQueue.Dequeue();
        }

        [Log(AttributeExclude = true)]
        public bool Any()
        {
            return taskQueue.Count > 0;
        }

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
