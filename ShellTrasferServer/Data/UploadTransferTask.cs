using Data;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Data
{
    public class UploadTransferTask : TransferTask
    {
        public UploadTransferTask(DownloadRequest downloadRequest, RemoteFileInfo remoteFileInfo, Action<object> callback) : base(TaskType.Upload, downloadRequest, remoteFileInfo, callback)
        {}

        public UploadTransferTask() : base(TaskType.Upload, null, null, null)
        { }


        [Log(AttributeExclude = true)]
        public override string ToString()
        {
            try
            {
                return string.Format("{0} {1} {2}",
                                                 TaskType,
                                                 RemoteFileInfo.FileName,
                                                 RemoteFileInfo.PathToSaveOnServer);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
