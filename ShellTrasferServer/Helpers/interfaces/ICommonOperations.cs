using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface ICommonOperations
    {
        void RemoveClient(string id, bool onlyFromServer = false);

        bool DeleteClientTask(string id, bool shellTask, int taksNumber, bool safeToPassLock = false);

        bool IsTransferingData();

        void StartTransferData();
    }
}
