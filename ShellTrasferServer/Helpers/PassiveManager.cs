using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class PassiveManager : IPassiveManager
    {
        public PassiveManager(IPassiveTransferDataHelper PassiveTransferDataHelper,
                              IPassiveShellHelper PassiveShellHelper,
                              IPassiveClientHelper PassiveClientHelper)
        {
            this.PassiveTransferDataHelper = PassiveTransferDataHelper;
            this.PassiveShellHelper = PassiveShellHelper;
            this.PassiveClientHelper = PassiveClientHelper;
        }

        public IPassiveTransferDataHelper PassiveTransferDataHelper { get; }
        public IPassiveShellHelper PassiveShellHelper { get; }
        public IPassiveClientHelper PassiveClientHelper { get; }
    }
}
