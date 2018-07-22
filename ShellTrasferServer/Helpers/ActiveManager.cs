using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class ActiveManager : IActiveManager
    {
        public ActiveManager(IActiveTransferDataHelper activeTransferDataHelper,
                             IActiveShellHelper activeShellHelper,
                             IActiveClientHelper activeClientHelper)
        {
            TransferDataHelper = activeTransferDataHelper;
            ShellHelper = activeShellHelper;
            ClientHelper = activeClientHelper;
        }

        public IActiveTransferDataHelper TransferDataHelper { get; }
        public IActiveShellHelper ShellHelper { get; }
        public IActiveClientHelper ClientHelper { get; }
    }
}
