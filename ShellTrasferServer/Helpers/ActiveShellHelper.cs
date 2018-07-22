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
    public class ActiveShellHelper : IActiveShellHelper
    {
        private readonly IAtomicOperation atomicOperation;
        private readonly IActiveClientLocks activeClientLocks;
        private readonly IUserClientManager userClientManager;
        private readonly IUserTaskQueue userTaskQueue;
        private readonly IMonitorHelper monitorHelper;
        private readonly ICommonOperations commonOperations;

        public ActiveShellHelper(IAtomicOperation atomicOperation,
                                 IActiveClientLocks activeClientLocks,
                                 IUserClientManager userClientManager,
                                 IUserTaskQueue userTaskQueue,
                                 IMonitorHelper monitorHelper,
                                 ICommonOperations commonOperations)
        {
            this.atomicOperation = atomicOperation;
            this.activeClientLocks = activeClientLocks;
            this.userClientManager = userClientManager;
            this.userTaskQueue = userTaskQueue;
            this.monitorHelper = monitorHelper;
            this.commonOperations = commonOperations;
        }

        public string ActiveNextCommand(string args)
        {
            return EnqueueWaitAndReturnCurrentPathLine(Constans.NextCommand, args);
        }

        public string ActiveClientRun()
        {
            return EnqueueWaitAndReturnCurrentPathLine(Constans.Run, string.Empty);
        }

        public void ClearQueue()
        {
            atomicOperation.PerformAsAtomicFunction(() =>
            {
                foreach (var client in userTaskQueue.ShellQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    commonOperations.DeleteClientTask(client, true, 1, true);
                }
                userTaskQueue.ShellQueue.Clear();
                foreach (var client in userTaskQueue.TransferTaskQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    commonOperations.DeleteClientTask(client, false, 1, true);
                }
                userTaskQueue.TransferTaskQueue.Clear();
            });
        }

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private string EnqueueWaitAndReturnCurrentPathLine(string command, string args)
        {
            var currentUserCallbacks = userClientManager.CallBacks;
            var shellQueue = userTaskQueue.ShellQueue;
            var deletedTasks = userClientManager.DeletedTasks;

            var waitForClientToExecute = true;
            var clientLock = new Object();
            activeClientLocks.Add(clientLock);
            var currentSelectedClient = string.Empty;
            var retBaseLine = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            atomicOperation.PerformAsAtomicFunction(() =>
            {
                selectedClient = userClientManager.SelectedClient;
                if (selectedClient == null || !currentUserCallbacks.ContainsKey(selectedClient))
                {
                    retBaseLine = "Error: Client does not exsits";
                    waitForClientToExecute = false;
                    return;
                }
                taskId = Guid.NewGuid().ToString();
                currentSelectedClient = userClientManager.SelectedClient;
                shellQueue[userClientManager.SelectedClient].Enqueue(new ShellTask(command, args,
                    (str) =>
                    {
                        retBaseLine = str;
                        waitForClientToExecute = false;
                        lock (clientLock)
                        {
                            monitorHelper.PulseAll(clientLock);
                        }
                        activeClientLocks.Remove(clientLock);
                    }, taskId));

                clientCallBack = currentUserCallbacks[selectedClient];
            });

            if (clientCallBack != null)
                try
                {
                    clientCallBack.CallBackFunction(selectedClient);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            else
                return "Client CallBack is Not Found";

            while (waitForClientToExecute)
            {
                if (deletedTasks.Contains(taskId))
                {
                    waitForClientToExecute = false;
                    retBaseLine = "Task were deleted";
                }

                lock (clientLock)
                {
                    monitorHelper.Wait(clientLock);
                }
            }
            return retBaseLine;
        }
    }
}
