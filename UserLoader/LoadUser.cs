using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace UserLoader
{
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

        public bool Any()
        {
            return taskQueue.Any();
        }

        private TaskQueue() { }

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
