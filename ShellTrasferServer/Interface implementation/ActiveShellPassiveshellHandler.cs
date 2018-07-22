using ShellTrasferServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Interface_implementation
{
    public class ActiveShellPassiveshellHandler : IActiveShellPassiveshell
    {
        private readonly IAtomicOperation _atomicOperation;

        public ActiveShellPassiveshellHandler(IAtomicOperation atomicOperation)
        {
            _atomicOperation = atomicOperation;
        }

        public bool IsTransferingData()
        {
            return _atomicOperation.IsTransferingData;
        }

        public void StartTransferData()
        {
            _atomicOperation.IsTransferingData = true;
        }
    }
}
