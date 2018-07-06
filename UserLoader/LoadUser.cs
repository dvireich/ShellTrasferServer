using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WcfLogger;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace UserLoader
{
    [WcfLogging]
    public class LoadUser : ILoadUser
    {
        bool ILoadUser.LoadUser(string id)
        {
            TaskQueue.Instance.AddToTaskQueue(id);
            return true;
        }
    }
}
