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
    public class ActiveManagerFactory : IActiveManagerFactory
    {
        public IActiveManager GetActiveManager()
        {
            return new ActiveManager(GetActiveTransferDataHelper(),
                                     GetActiveShellHelper(),
                                     GetActiveClientHelper());
        }

        private ICommonOperations GetCommonOperations()
        {
            return new CommonOperations(ClientManager.Instance.CurretUserClientManager,
                                        TaskQueue.Instance.CurrentUserTaskQueue,
                                        UserAtomicOperation.Instance.AtomicOperation,
                                        ShellTaskLockManager.Instance.CurrentUserLockMannager);
        }

        private IActiveTransferDataHelper GetActiveTransferDataHelper()
        {
            return new ActiveTransferDataHelper(UserAtomicOperation.Instance.AtomicOperation,
                                                ClientManager.Instance.CurretUserClientManager,
                                                TaskQueue.Instance.CurrentUserTaskQueue,
                                                FileMannager.Instance.CurrentUserFileMannager,
                                                new MonitorHelper(),
                                                new FileHelper(),
                                                new DirectoryHelper());
        }

        private IActiveShellHelper GetActiveShellHelper()
        {
            return new ActiveShellHelper(UserAtomicOperation.Instance.AtomicOperation,
                                         ShellTaskLockManager.Instance.CurrentUserLockMannager,
                                         ClientManager.Instance.CurretUserClientManager,
                                         TaskQueue.Instance.CurrentUserTaskQueue,
                                         new MonitorHelper(),
                                         GetCommonOperations());
        }

        private IActiveClientHelper GetActiveClientHelper()
        {
            return new ActiveClientHelper(UserAtomicOperation.Instance.AtomicOperation,
                                          ClientManager.Instance.CurretUserClientManager,
                                          TaskQueue.Instance.CurrentUserTaskQueue,
                                          GetCommonOperations());
        }
    }
}
