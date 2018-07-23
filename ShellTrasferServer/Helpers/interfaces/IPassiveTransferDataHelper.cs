using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IPassiveTransferDataHelper
    {
        bool HasUploadCommand(string id);

        bool HasDownloadCommand(string id);

        DownloadRequest PassiveGetDownloadFile(DownloadRequest id);

        void PassiveDownloadedFile(RemoteFileInfo request);

        RemoteFileInfo PassiveGetUploadFile(DownloadRequest id);

        void PassiveUploadedFile(string id, string taskId);

        void ErrorUploadDownload(string id, string taskId, string response);

        bool IsTransferingData();

        void StartTransferData();
    }
}
