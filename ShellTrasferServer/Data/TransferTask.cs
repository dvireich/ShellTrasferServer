using PostSharp.Patterns.Diagnostics;
using ShellTrasferServer;
using System;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class TransferTask
    {
        public TaskType TaskType;
        public DownloadRequest DownloadRequest;
        public RemoteFileInfo RemoteFileInfo;
        public Action<object> Callback;

        public TransferTask(TaskType taskType, DownloadRequest downloadRequest, RemoteFileInfo remoteFileInfo, Action<object> callback)
        {
            TaskType = taskType;
            DownloadRequest = downloadRequest;
            RemoteFileInfo = remoteFileInfo;
            Callback = callback;
        }
    }
}
