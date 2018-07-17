using Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WcfLogger;

namespace ShellTrasferServer
{
    [WcfLogging]
    public class PassiveShell : ActiveShellPassiveshell, IPassiveShell
    {
        readonly string version = "11";
        const string closeShell = "CloseShell";

        #region IPassiveShell Implemantations

        public bool PassiveClientRun(string id, string taskId, string baseLine)
        {
            var currentUserShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            if (!AllowOnlySelectedClient(id))
                return false;
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!currentUserShellQueue.ContainsKey(id) ||
                !currentUserShellQueue[id].Any() ||
                currentUserShellQueue[id].Peek().TaskId != taskId)
                return false;
            var shellTask = currentUserShellQueue[id].Dequeue();
            var callback = shellTask.Callback;
            callback(baseLine);
            return true;
        }

        public bool HasCommand(string id)
        {
            var currentUserDeletedTasks = ClientManager.Instance.CurretUserClientManager.Deleted;
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;

            if (currentUserDeletedTasks.Contains(id))
            {
                return true;
            }
            return AllowOnlySelectedClient(id) ?
                  currentTransferQueue.ContainsKey(id) && currentShellQueue[id].Count > 0 :
                  false;
        }

        public Tuple<string, string, string> PassiveNextCommand(string id)
        {
            var currentUserDeletedTasks = ClientManager.Instance.CurretUserClientManager.Deleted;
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;

            if (currentUserDeletedTasks.Contains(id))
            {
                return new Tuple<string, string, string>(closeShell, "", "");
            }
            //In case that between the has command and the passive client get command the task has been deleted
            if (currentShellQueue[id].Count == 0)
                return new Tuple<string, string, string>("", "", "");

            var task = currentShellQueue[id].Peek();
            return new Tuple<string, string, string>(task.Command, task.Args, task.TaskId);
        }

        public void CommandResponse(string id, string taskId, string baseLine)
        {
            var currentUserDeletedTasks = ClientManager.Instance.CurretUserClientManager.Deleted;
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            if (baseLine == "CleanId")
            {
                //unique case that the passive client got some exception and he want to start over, so he delete his
                //previous id and start fresh 

                //This is not part of ActiveClient command process. this was promoted by the passiveClient so there is
                //no conflict in the locks.

                //prevent the active user using the Selected Client or the CallBack Dictionary when in the mean time 
                //is been deleted by the passive client func. Let the first caller finish his operation and then perform
                //the second call
                currentUserAtomicOperation.PerformAsAtomicFunction(() =>
                {

                    RemoveClient(id, true);
                });
                return;
            }
            //When passive client been deleted, he get a signal from the callback and in hasCommand get true
            //and in NextCommand he get new mission that is not in the missionQueue so we dont need to dequeue
            if (currentUserDeletedTasks.Contains(id))
                return;
            //In case that between the has command and the passive client get command the task has been deleted
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!currentShellQueue.ContainsKey(id) ||
                !currentShellQueue[id].Any() ||
                currentShellQueue[id].Peek().TaskId != taskId)
                return;
            var task = currentShellQueue[id].Dequeue();
            var callback = task.Callback;
            callback(baseLine);
        }

        public bool HasUploadCommand(string id)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            return AllowOnlySelectedClient(id) ?
                currentTransferQueue.ContainsKey(id) &&
                currentTransferQueue[id].Count > 0 &&
                currentTransferQueue[id].Peek().TaskType == TaskType.Upload :
                false;
        }

        public bool HasDownloadCommand(string id)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            return AllowOnlySelectedClient(id) ?
                   currentTransferQueue.ContainsKey(id) &&
                   currentTransferQueue[id].Count > 0 &&
                   currentTransferQueue[id].Peek().TaskType == TaskType.Download :
                   false;
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public DownloadRequest PassiveGetDownloadFile(DownloadRequest id)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;

            //In case that between the has command and the passive client get command the task has been deleted
            if (!currentTransferQueue.ContainsKey(id.id) || !currentTransferQueue[id.id].Any())
            {
                return new DownloadRequest()
                {
                    taskId = Guid.Empty.ToString()
                };
            }
            var downloadTask = currentTransferQueue[id.id].Peek();
            var req = downloadTask.DownloadRequest;
            return req;
        }

        [WcfLogging(LogArguments = false)]
        public void PassiveDownloadedFile(RemoteFileInfo request)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;

            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!currentTransferQueue.ContainsKey(request.id) ||
                !currentTransferQueue[request.id].Any() ||
                currentTransferQueue[request.id].Peek().DownloadRequest.taskId != request.taskId)
                return;

            //We will save all the byte chunks in temp file, its propebly will became to big with all the add actions
            //for some data strcture so we probebly will get SystemOutOfMemory exception
            if (currentFileManager.FileStream == null)
            {
                var newGuid = Guid.NewGuid();
                currentFileManager.Path = Path.Combine(Path.GetTempPath(), newGuid.ToString(), request.FileName);
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), newGuid.ToString())))
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), newGuid.ToString()));
                currentFileManager.FileStream = new FileStream(currentFileManager.Path,
                                                               FileMode.Create,
                                                               FileAccess.ReadWrite,
                                                               FileShare.ReadWrite);
                currentFileManager.FileSize = request.FileSize;
            }
            //When the passive client finish sending all the chumks he will signal with the request.FileEnded flag
            if (!request.FileEnded)
            {
                currentFileManager.FileStream.Write(request.FileByteStream, 0, request.FileByteStream.Length);
                currentFileManager.FileStream.Flush();
            }
            else
            {
                //download from pasive client ended. now we need to notify the active client
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;

                var uploadTask = currentTransferQueue[request.id].Dequeue();
                var callback = uploadTask.Callback;
                callback(true);
            }

        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public RemoteFileInfo PassiveGetUploadFile(DownloadRequest id)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            //In case that between the has command and the passive client get command the task has been deleted
            if (!currentTransferQueue.ContainsKey(id.id) || !currentTransferQueue[id.id].Any())
            {
                return new RemoteFileInfo()
                {
                    taskId = Guid.Empty.ToString(),
                    FileName = string.Empty,
                    FileByteStream = new byte[0],
                    FileEnded = true,
                    id = id.id,
                    FileSize = currentFileManager.FileSize
                };
            }

            var uploadTask = currentTransferQueue[id.id].Peek();
            var req = uploadTask.RemoteFileInfo;

            //We dont want pices of byte in the wrong order
            return currentUserAtomicOperation.PerformAsAtomicUpload<RemoteFileInfo>(() =>
            {
                //We have still download in process and we have chunks to send
                //Must be in this order. the MoveNext() first in order to load the first chunk
                if (currentFileManager.IsUploading && currentFileManager.EnumerableChunk.MoveNext())
                {
                    return new RemoteFileInfo()
                    {
                        FileByteStream = currentFileManager.EnumerableChunk.Current.Item1,
                        FileName = req.FileName,
                        PathToSaveOnServer = req.PathToSaveOnServer,
                        FileEnded = false,
                        Length = currentFileManager.EnumerableChunk.Current.Item2,
                        taskId = req.taskId,
                        id = id.id,
                        FileSize = currentFileManager.FileSize
                    };
                }

                //We dont have chunks to send so end the download 
                try
                {
                    File.Delete(currentFileManager.Path);
                }
                catch { }
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = req.FileName,
                    PathToSaveOnServer = req.PathToSaveOnServer,
                    FileEnded = true,
                    taskId = req.taskId,
                    id = id.id,
                    FileSize = currentFileManager.FileSize
                };
            });
        }

        public void PassiveUploadedFile(string id, string taskId)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;

            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!currentTransferQueue.ContainsKey(id) ||
                !currentTransferQueue[id].Any() ||
                currentTransferQueue[id].Peek().RemoteFileInfo.taskId != taskId)
                return;
            var uploadTask = currentTransferQueue[id].Dequeue();
            var callback = uploadTask.Callback;
            callback(true);
        }

        public void ErrorUploadDownload(string id, string taskId, string response)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;

            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return

            //Delete the temp file
            if (currentFileManager.FileStream != null)
            {
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;
            }
            try
            {
                if (File.Exists(currentFileManager.Path))
                    File.Delete(currentFileManager.Path);
            }
            catch { }
            if (!currentTransferQueue.ContainsKey(id) ||
                !currentTransferQueue[id].Any() ||
                currentTransferQueue[id].Peek().RemoteFileInfo.taskId != taskId)
                return;
            var task = currentTransferQueue[id].Dequeue();
            var callback = task.Callback;
            callback(response);
        }

        public void ErrorNextCommand(string id, string taskId, string response)
        {
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            var currentDeletedTasks = ClientManager.Instance.CurretUserClientManager.Deleted;

            //When passive client been deleted, he get a signal from the callback and in hasCommand get true
            //and in NextCommand he get new mission that is not in the missionQueue so we dont need to dequeue

            //If the PassiveClient was deleted the get signal and check if he has command to execute
            //then in NextCommand he get a CloseShell command, if something go wrong the client will end up here
            //but as far as the server concern the client deleted from his data.
            if (currentDeletedTasks.Contains(id))
            {
                return;
            }
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!currentShellQueue.ContainsKey(id) ||
                !currentShellQueue[id].Any() ||
                currentShellQueue[id].Peek().TaskId != taskId)
                return;
            var task = currentShellQueue[id].Dequeue();
            var callback = task.Callback;
            callback(response);
        }

        public bool Subscribe(string id, string version, string name)
        {
            if (version != this.version)
                return false;

            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            return currentUserAtomicOperation.PerformAsAtomicSubscribe(() =>
            {
                var currentClientManager = ClientManager.Instance.CurretUserClientManager;
                var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
                var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
                var currentUserCallbacks = currentClientManager.CallBacks;


                if (!currentUserCallbacks.ContainsKey(id))
                {
                    currentUserCallbacks[id] = null;
                    currentClientManager.NickNames[id] = string.IsNullOrWhiteSpace(name) ? "None" : name;
                    currentShellQueue[id] = new Queue<ShellTask>();
                    currentTransferQueue[id] = new Queue<TransferTask>();
                    if (currentUserCallbacks.Count == 1)
                        DefineSelectedClient(id);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        #endregion IPassiveShell Implemantations

        private bool AllowOnlySelectedClient(string id)
        {
            return ClientManager.Instance.CurretUserClientManager.SelectedClient == id;
        }

        private void DefineSelectedClient(string id)
        {
            ClientManager.Instance.CurretUserClientManager.SelectedClient = id;
        }
    }
}
