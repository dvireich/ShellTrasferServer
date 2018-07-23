using Data;
using ShellTrasferServer.Data;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    class PassiveManagerFactory : IPassiveManagerFactory
    {
        public IPassiveManager GetPassiveManager()
        {
            return new PassiveManager(GetPassiveTransferDataHelper(),
                                      GetPassiveShellHelper(),
                                      GetPassiveClientHelper());
        }

        private IPassiveShellHelper GetPassiveShellHelper()
        {
            return new PassiveShellHelper(TaskQueue.Instance.CurrentUserTaskQueue,
                                          ClientManager.Instance.CurretUserClientManager);
        }

        private IPassiveTransferDataHelper GetPassiveTransferDataHelper()
        {
            return new PassiveTransferDataHelper(TaskQueue.Instance.CurrentUserTaskQueue,
                                                 ClientManager.Instance.CurretUserClientManager,
                                                 FileMannager.Instance.CurrentUserFileMannager,
                                                 new FileHelper(),
                                                 UserAtomicOperation.Instance.AtomicOperation,
                                                 new DirectoryHelper(),
                                                 GetCommonOperations());
        }

        private IPassiveClientHelper GetPassiveClientHelper()
        {
            return new PassiveClientHelper(UserAtomicOperation.Instance.AtomicOperation,
                                           ClientManager.Instance.CurretUserClientManager,
                                           TaskQueue.Instance.CurrentUserTaskQueue,
                                           GetCommonOperations());
        }

        private ICommonOperations GetCommonOperations()
        {
            return new CommonOperations(ClientManager.Instance.CurretUserClientManager,
                                        TaskQueue.Instance.CurrentUserTaskQueue,
                                        UserAtomicOperation.Instance.AtomicOperation,
                                        ShellTaskLockManager.Instance.CurrentUserLockMannager);
        }
    }
}
