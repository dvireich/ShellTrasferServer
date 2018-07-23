using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IActiveTransferDataHelper
    {
        RemoteFileInfo ActiveDownloadFile(DownloadRequest request);

        RemoteFileInfo ActiveUploadFile(RemoteFileInfo request);

        bool IsTransferingData();

        void StartTransferData();
    }
}
