using Data;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Data
{
    public class DownloadTransferTask : TransferTask
    {
        public DownloadTransferTask(DownloadRequest downloadRequest, RemoteFileInfo remoteFileInfo, Action<object> callback) : base(TaskType.Download, downloadRequest, remoteFileInfo, callback)
        {
        }

        public DownloadTransferTask() : base(TaskType.Download, null, null, null)
        {

        }

        [Log(AttributeExclude = true)]
        public override string ToString()
        {
            try
            {
                return string.Format("{0} {1} {2} {3}",
                                                TaskType,
                                                DownloadRequest.FileName,
                                                DownloadRequest.PathInServer,
                                                DownloadRequest.PathToSaveInClient);
            }
            catch
            {
                return string.Empty;
            }
            
        }
    }
}
