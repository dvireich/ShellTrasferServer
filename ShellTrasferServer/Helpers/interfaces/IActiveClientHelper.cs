using Data;
using ShellTrasferServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IActiveClientHelper
    {
        bool ActiveSetNickName(string id, string nickName);

        bool ClearAllData(string id);

        bool SelectClient(string id);

        bool ActiveCloseClient(string id);

        string GetStatus();

        bool DeleteClientTask(string id, bool shellTask, int taksNumber);
    }
}
