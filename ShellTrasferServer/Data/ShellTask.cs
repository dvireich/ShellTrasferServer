using PostSharp.Patterns.Diagnostics;
using System;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class ShellTask
    {
        public string Command;
        public string Args;
        public string TaskId;
        public Action<string> Callback;

        public ShellTask(string command, string args, Action<string> callback, string taskId)
        {
            Command = command;
            Args = args;
            Callback = callback;
            TaskId = taskId;
        }
    }

    
}
