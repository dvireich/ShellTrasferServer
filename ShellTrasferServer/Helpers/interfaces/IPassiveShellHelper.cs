using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IPassiveShellHelper
    {
        bool HasShellCommand(string id);

        Tuple<string, string, string> PassiveNextCommand(string id);

        void CommandResponse(string id, string taskId, string baseLine);

        bool PassiveClientRun(string id, string taskId, string baseLine);

        void ErrorNextCommand(string id, string taskId, string response);
    }
}
