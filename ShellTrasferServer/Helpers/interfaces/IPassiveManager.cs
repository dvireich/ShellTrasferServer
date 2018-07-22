using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IPassiveManager
    {
        IPassiveTransferDataHelper PassiveTransferDataHelper { get; }

        IPassiveShellHelper PassiveShellHelper { get; }

        IPassiveClientHelper PassiveClientHelper { get; }
    }
}
