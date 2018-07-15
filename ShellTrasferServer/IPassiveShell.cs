using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    [ServiceContract]
    public interface IPassiveShell : IActiveShellPassiveshell
    {
        [OperationContract]
        bool PassiveClientRun(string id, string taskId, string baseLine);
        [OperationContract]
        Tuple<string, string, string> PassiveNextCommand(string id);
        [OperationContract]
        void CommandResponse(string id, string taskId, string baseLine);
        [OperationContract]
        bool HasCommand(string id);
        [OperationContract]
        bool HasUploadCommand(string id);
        [OperationContract]
        bool HasDownloadCommand(string id);
        [OperationContract]
        void PassiveDownloadedFile(RemoteFileInfo request);
        [OperationContract]
        DownloadRequest PassiveGetDownloadFile(DownloadRequest id);
        [OperationContract]
        RemoteFileInfo PassiveGetUploadFile(DownloadRequest id);
        [OperationContract]
        void PassiveUploadedFile(string id, string taskId);
        [OperationContract]
        void ErrorUploadDownload(string id, string taskId, string response);
        [OperationContract]
        bool Subscribe(string id, string version, string nickName);
        [OperationContract]
        void ErrorNextCommand(string id, string taskId, string response);
    }
}
