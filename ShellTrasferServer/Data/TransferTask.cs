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

        public override string ToString()
        {
            if (TaskType == TaskType.Upload)
            {
                return string.Format("{0} {1} {2}",
                                                 TaskType,
                                                 RemoteFileInfo.FileName,
                                                 RemoteFileInfo.PathToSaveOnServer);
            }
            else if (TaskType == TaskType.Download)
            {
                return string.Format("{0} {1} {2} {3}",
                                                TaskType,
                                                DownloadRequest.FileName,
                                                DownloadRequest.PathInServer,
                                                DownloadRequest.PathToSaveInClient);
            }

            return string.Empty;
        }
    }
}
