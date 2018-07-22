using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IActiveManager
    {
        IActiveTransferDataHelper TransferDataHelper { get; }

        IActiveShellHelper ShellHelper { get; }

        IActiveClientHelper ClientHelper { get; }
    }
}
