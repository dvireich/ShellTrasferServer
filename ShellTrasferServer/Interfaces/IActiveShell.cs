using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    [ServiceContract]
    public interface IActiveShell : IActiveShellPassiveshell
    {
        [OperationContract]
        string ActiveNextCommand(string command);
        [OperationContract]
        string ActiveClientRun();
        [OperationContract]
        RemoteFileInfo ActiveDownloadFile(DownloadRequest request);
        [OperationContract]
        RemoteFileInfo ActiveUploadFile(RemoteFileInfo request);
        [OperationContract]
        void ClearQueue();
        [OperationContract]
        string GetStatus();
        [OperationContract]
        bool SelectClient(string id);
        [OperationContract]
        bool ActiveCloseClient(string id);
        [OperationContract]
        bool ClearAllData(string id);
        [OperationContract]
        bool DeleteClientTask(string id, bool shellTask, int taksNumber);
        [OperationContract]
        bool ActiveSetNickName(string id, string nickName);
    }
}
