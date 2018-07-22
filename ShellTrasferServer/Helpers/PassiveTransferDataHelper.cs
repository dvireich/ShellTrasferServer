using Data;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class PassiveTransferDataHelper : IPassiveTransferDataHelper
    {
        private readonly IUserTaskQueue userTaskQueue;
        private readonly IUserClientManager userClientManager;
        private readonly IUserFileManager userFileManager;
        private readonly IFileHelper fileHelper;
        private readonly IAtomicOperation atomicOperation;
        private readonly IDirectoryHelper directoryHelper;

        public PassiveTransferDataHelper(IUserTaskQueue userTaskQueue,
                                         IUserClientManager userClientManager,
                                         IUserFileManager userFileManager,
                                         IFileHelper fileHelper,
                                         IAtomicOperation atomicOperation,
                                         IDirectoryHelper directoryHelper)
        {
            this.userTaskQueue = userTaskQueue;
            this.userClientManager = userClientManager;
            this.userFileManager = userFileManager;
            this.fileHelper = fileHelper;
            this.atomicOperation = atomicOperation;
            this.directoryHelper = directoryHelper;
        }

        public bool HasUploadCommand(string id)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;
            return AllowOnlySelectedClient(id) ?
                currentTransferQueue.ContainsKey(id) &&
                currentTransferQueue[id].Count > 0 &&
                currentTransferQueue[id].Peek().TaskType == TaskType.Upload :
                false;
        }

        public void PassiveUploadedFile(string id, string taskId)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

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
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return

            //Delete the temp file
            if (userFileManager.FileStream != null)
            {
                userFileManager.FileStream.Close();
                userFileManager.FileStream = null;
            }
            try
            {
                if (fileHelper.Exists(userFileManager.Path))
                    fileHelper.Delete(userFileManager.Path);
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

        public bool HasDownloadCommand(string id)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;
            return AllowOnlySelectedClient(id) ?
                   currentTransferQueue.ContainsKey(id) &&
                   currentTransferQueue[id].Count > 0 &&
                   currentTransferQueue[id].Peek().TaskType == TaskType.Download :
                   false;
        }

        private bool CheckIfTaskDeletedAndReturn(RemoteFileInfo request)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

            if (!currentTransferQueue.ContainsKey(request.id) ||
                !currentTransferQueue[request.id].Any() ||
                currentTransferQueue[request.id].Peek().DownloadRequest.taskId != request.taskId)
                return true;

            return false;
        }

        private void CreateTmpFileIfNeeded(RemoteFileInfo request)
        {
            if (userFileManager.FileStream != null) return;

            var newGuid = Guid.NewGuid();
            userFileManager.Path = Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString(), request.FileName);
            if (!directoryHelper.Exists(Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString())))
            {
                directoryHelper.CreateDirectory(Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString()));
            }  
            userFileManager.FileStream = fileHelper.GetFileStream(userFileManager.Path,
                                                           FileMode.Create,
                                                           FileAccess.ReadWrite,
                                                           FileShare.ReadWrite);
            userFileManager.FileSize = request.FileSize;
        }

        public void PassiveDownloadedFile(RemoteFileInfo request)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

            if (CheckIfTaskDeletedAndReturn(request)) return;

            CreateTmpFileIfNeeded(request);

            //When the passive client finish sending all the chumks he will signal with the request.FileEnded flag
            if (!request.FileEnded)
            {
                userFileManager.FileStream.Write(request.FileByteStream, 0, request.FileByteStream.Length);
                userFileManager.FileStream.Flush();
            }
            else
            {
                //download from pasive client ended. now we need to notify the active client
                userFileManager.FileStream.Close();
                userFileManager.FileStream = null;

                var uploadTask = currentTransferQueue[request.id].Dequeue();
                var callback = uploadTask.Callback;
                callback(true);
            }
        }

        public DownloadRequest PassiveGetDownloadFile(DownloadRequest id)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

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

        private bool CheckIfTaskDeletedAndReturn(DownloadRequest downloadRequest, out RemoteFileInfo result)
        {
            result = null;
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

            if (currentTransferQueue.ContainsKey(downloadRequest.id) && currentTransferQueue[downloadRequest.id].Any()) return false;

            result = new RemoteFileInfo()
            {
                taskId = Guid.Empty.ToString(),
                FileName = string.Empty,
                FileByteStream = new byte[0],
                FileEnded = true,
                id = downloadRequest.id,
                FileSize = userFileManager.FileSize
            };
            return true;
        }

        private bool CheckIfWeHaveChunksAndReturn(string id, RemoteFileInfo req, out RemoteFileInfo result)
        {
            result = null;
            //We have still download in process and we have chunks to send
            //Must be in this order. the MoveNext() first in order to load the first chunk
            if (!userFileManager.IsUploading || !userFileManager.EnumerableChunk.MoveNext()) return false;
            result = new RemoteFileInfo()
            {
                FileByteStream = userFileManager.EnumerableChunk.Current.Item1,
                FileName = req.FileName,
                PathToSaveOnServer = req.PathToSaveOnServer,
                FileEnded = false,
                Length = userFileManager.EnumerableChunk.Current.Item2,
                taskId = req.taskId,
                id = id,
                FileSize = userFileManager.FileSize
            };

            return true;
        }

        private RemoteFileInfo DeleteTmpFileAndSendUploadEndNotification(string id, RemoteFileInfo req)
        {
            try
            {
                fileHelper.Delete(userFileManager.Path);
            }
            catch { }
            return new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = req.FileName,
                PathToSaveOnServer = req.PathToSaveOnServer,
                FileEnded = true,
                taskId = req.taskId,
                id = id,
                FileSize = userFileManager.FileSize
            };
        }

        public RemoteFileInfo PassiveGetUploadFile(DownloadRequest id)
        {
            var currentTransferQueue = userTaskQueue.TransferTaskQueue;

            if(CheckIfTaskDeletedAndReturn(id, out var deletedTask)) return deletedTask;

            var uploadTask = currentTransferQueue[id.id].Peek();
            var req = uploadTask.RemoteFileInfo;

            //We dont want pices of byte in the wrong order
            return atomicOperation.PerformAsAtomicUpload(() =>
            {
                if (CheckIfWeHaveChunksAndReturn(id.id, req, out var chunk)) return chunk;

                //We dont have chunks to send so end the download 
                return DeleteTmpFileAndSendUploadEndNotification(id.id, req);
            });
        }

        private bool AllowOnlySelectedClient(string id)
        {
            return userClientManager.SelectedClient == id;
        }
    }
}
