using Data;
using ShellTrasferServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WcfLogger;

namespace ShellTrasferServer
{
    [WcfLogging]
    public class ActiveShellPassiveshell : IActiveShellPassiveshell
    {
        public void StartTransferData()
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;
            currentUserAtomicOperation.IsTransferingData = true;
        }

        public bool IsTransferingData()
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;
            return currentUserAtomicOperation.IsTransferingData;
        }

        public void RemoveClient(string id, bool onlyFromServer = false)
        {
            var currentClientManager = ClientManager.Instance.CurretUserClientManager;
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentUserCallbacks = currentClientManager.CallBacks;
            var currentUserStatusCallbacks = currentClientManager.StatusCallBacks;
            var currentUserDeleted = ClientManager.Instance.CurretUserClientManager.Deleted;

            if (id == null || !currentUserCallbacks.ContainsKey(id))
            {
                return;
            }
            var selectedClient = currentClientManager.SelectedClient;
            var clientCallBack = currentUserCallbacks[id];
            currentUserCallbacks.TryRemove(id, out ICallBack iCallBackObj);
            currentUserStatusCallbacks.TryRemove(id, out iCallBackObj);
            currentClientManager.NickNames.Remove(id);
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
                    currentClientManager.SelectedClient = currentUserCallbacks.First().Key;
                }
                else if (currentUserCallbacks.Count == 0 && id == selectedClient)
                {
                    currentClientManager.SelectedClient = null;
                }
            }
        }

        public bool DeleteClientTask(string id, bool shellTask, int taksNumber, bool safeToPassLock = false)
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;
            var currentLockManager = ShellTaskLockManager.Instance.CurrentUserLockMannager;

            return currentUserAtomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                if (shellTask)
                {
                    var currentUserShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
                    var deletedTasks = ClientManager.Instance.CurretUserClientManager.DeletedTasks;
                    if (currentUserShellQueue.ContainsKey(id) && currentUserShellQueue[id].Count > taksNumber - 1)
                    {
                        var deleted = currentUserShellQueue[id].ElementAt(taksNumber - 1);
                        currentUserShellQueue[id] = currentUserShellQueue[id].DeleteAt(taksNumber - 1);
                        deletedTasks.Add(deleted.TaskId);
                        currentLockManager.PulseAll();
                        return true;
                    }
                }
                else
                {
                    var currentUserTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
                    var deletedTasks = ClientManager.Instance.CurretUserClientManager.DeletedTasks;
                    if (currentUserTransferQueue.ContainsKey(id) && currentUserTransferQueue[id].Count > taksNumber - 1)
                    {
                        var deleted = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue[id].ElementAt(taksNumber - 1);
                        currentUserTransferQueue[id] = currentUserTransferQueue[id].DeleteAt(taksNumber - 1);
                        //This is a hash set so it wont be added twice
                        deletedTasks.Add(deleted.DownloadRequest.taskId);
                        deletedTasks.Add(deleted.RemoteFileInfo.taskId);
                        currentLockManager.PulseAll();
                        return true;
                    }
                }
                return false;
            }, safeToPassLock);
        }
    }
}
