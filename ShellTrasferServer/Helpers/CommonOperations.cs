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
    public class CommonOperations : ICommonOperations
    {
        private readonly IUserClientManager userClientManager;
        private readonly IUserTaskQueue userTaskQueue;
        private readonly IAtomicOperation atomicOperation;
        private readonly IActiveClientLocks activeClientLocks;

        public CommonOperations(IUserClientManager userClientManager,
                                IUserTaskQueue userTaskQueue,
                                IAtomicOperation atomicOperation,
                                IActiveClientLocks activeClientLocks)
        {
            this.userClientManager = userClientManager;
            this.userTaskQueue = userTaskQueue;
            this.atomicOperation = atomicOperation;
            this.activeClientLocks = activeClientLocks;
        }

        public void RemoveClient(string id, bool onlyFromServer = false)
        {
            var currentShellQueue = userTaskQueue.ShellQueue;
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;
            var currentUserCallbacks = userClientManager.CallBacks;
            var currentUserStatusCallbacks = userClientManager.StatusCallBacks;
            var currentUserDeleted = userClientManager.Deleted;

            if (id == null || !currentUserCallbacks.ContainsKey(id))
            {
                return;
            }
            var selectedClient = userClientManager.SelectedClient;
            var clientCallBack = currentUserCallbacks[id];
            currentUserCallbacks.TryRemove(id, out ICallBack iCallBackObj);
            currentUserStatusCallbacks.TryRemove(id, out iCallBackObj);
            userClientManager.NickNames.Remove(id);
            Queue<ShellTask> shelElement;
            shelElement = currentShellQueue[id];
            if (shelElement != null)
                foreach (var element in shelElement)
                {
                    //In all removeClient invokes its in UserAtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, true, 1, true);
                }
            currentShellQueue.TryRemove(id, out shelElement);
            Queue<TransferTask> transferElement;
            transferElement = currentTransferQueue[id];
            if (transferElement != null)
                foreach (var element in transferElement)
                {
                    //In all removeClient invokes its in UserAtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, false, 1, true);
                }
            currentTransferQueue.TryRemove(id, out transferElement);
            currentUserDeleted.Add(id);
            try
            {
                if (!onlyFromServer)
                {
                    clientCallBack.CallBackFunction(id);
                    //Must do 2 times, the first not doing anything
                    // clientCallBack.CallBackFunction(id);
                }

            }
            catch { }
            finally
            {
                if (currentUserCallbacks.Count > 0 && id == selectedClient)
                {
                    userClientManager.SelectedClient = currentUserCallbacks.First().Key;
                }
                else if (currentUserCallbacks.Count == 0 && id == selectedClient)
                {
                    userClientManager.SelectedClient = null;
                }
            }
        }

        public bool DeleteClientTask(string id, bool shellTask, int taksNumber, bool safeToPassLock = false)
        {
            return atomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                if (shellTask)
                {
                    var currentUserShellQueue = userTaskQueue.ShellQueue;
                    var deletedTasks = userClientManager.DeletedTasks;
                    if (currentUserShellQueue.ContainsKey(id) && currentUserShellQueue[id].Count > taksNumber - 1)
                    {
                        var deleted = currentUserShellQueue[id].ElementAt(taksNumber - 1);
                        currentUserShellQueue[id] = currentUserShellQueue[id].DeleteAt(taksNumber - 1);
                        deletedTasks.Add(deleted.TaskId);
                        activeClientLocks.PulseAll();
                        return true;
                    }
                }
                else
                {
                    var currentUserTransferQueue = userTaskQueue.TransferTaskQueue;
                    var deletedTasks = userClientManager.DeletedTasks;
                    if (currentUserTransferQueue.ContainsKey(id) && currentUserTransferQueue[id].Count > taksNumber - 1)
                    {
                        var deleted = userTaskQueue.TransferTaskQueue[id].ElementAt(taksNumber - 1);
                        currentUserTransferQueue[id] = currentUserTransferQueue[id].DeleteAt(taksNumber - 1);
                        //This is a hash set so it wont be added twice
                        deletedTasks.Add(deleted.DownloadRequest.taskId);
                        deletedTasks.Add(deleted.RemoteFileInfo.taskId);
                        activeClientLocks.PulseAll();
                        return true;
                    }
                }
                return false;
            }, safeToPassLock);
        }

        public bool IsTransferingData()
        {
            return atomicOperation.IsTransferingData;
        }

        public void StartTransferData()
        {
            atomicOperation.IsTransferingData = true;
        }
    }
}
