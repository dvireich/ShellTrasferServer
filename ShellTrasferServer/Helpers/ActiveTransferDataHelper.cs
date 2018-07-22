using Data;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WcfLogger;

namespace ShellTrasferServer.Helpers
{
    public class ActiveTransferDataHelper : IActiveTransferDataHelper
    {
        private readonly IAtomicOperation atomicOperation;
        private readonly IUserClientManager userClientManager;
        private readonly IUserTaskQueue userTaskQueue;
        private readonly IUserFileManager userFileManager;
        private readonly IFileHelper fileHelper;
        private readonly IDirectoryHelper directoryHelper;

        public ActiveTransferDataHelper(IAtomicOperation atomicOperation,
                                        IUserClientManager userClientManager,
                                        IUserTaskQueue userTaskQueue,
                                        IUserFileManager userFileManager,
                                        IMonitorHelper monitorHelper,
                                        IFileHelper fileHelper,
                                        IDirectoryHelper directoryHelper)
        {
            this.atomicOperation = atomicOperation;
            this.userClientManager = userClientManager;
            this.userTaskQueue = userTaskQueue;
            this.userFileManager = userFileManager;
            this.fileHelper = fileHelper;
            this.directoryHelper = directoryHelper;
        }

        private bool CheckIfDownloadErrorAndReturn(out RemoteFileInfo result)
        {
            result = null;
            if (!userFileManager.Error) return false;

            userFileManager.Error = false;
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = string.Format("Error Download File: {0}", userFileManager.ErrorMessage),
                PathToSaveOnServer = ""
            };
            return true;
        }

        private bool CheckIfWeStillBufferingAndReturn(DownloadRequest request, out RemoteFileInfo result)
        {
            result = null;
            if (request.NewStart || !userFileManager.Buffering) return false;


            var fileSize = userFileManager.FileStream != null ? double.Parse(userFileManager.FileSize) : 0;
            var precent = userFileManager.FileStream != null ? ((userFileManager.FileStream.Position / fileSize) * 100) : 0;
            precent = precent > 100 ? 100 : precent;
            var messagePrecent = string.Format("Buffering File in Server Memory {0} %", (long)precent);
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = messagePrecent,
                PathToSaveOnServer = ""
            };
            return true;
        }

        private bool CheckWeStillDownloadingAndHaveChunksToRead(DownloadRequest request, out RemoteFileInfo result)
        {
            result = null;
            if (request.NewStart || (userFileManager.IsDownloding && userFileManager.EnumerableChunk.MoveNext())) return false;

            try
            {
                var path = userFileManager.Path;
                if (fileHelper.Exists(path))
                    fileHelper.Delete(path);
            }
            catch { }
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = request.FileName,
                PathToSaveOnServer = request.PathToSaveInClient,
                FileEnded = true
            };
            return true;
        }

        private bool CheckWeHaveDownloadInProcessAndSendChunk(DownloadRequest request, out RemoteFileInfo result)
        {
            result = null;
            if (request.NewStart || !userFileManager.IsDownloding) return false;

            result = new RemoteFileInfo()
            {
                FileByteStream = userFileManager.EnumerableChunk.Current.Item1,
                FileName = request.FileName,
                PathToSaveOnServer = string.Empty,
                FileEnded = false,
                Length = userFileManager.EnumerableChunk.Current.Item2,
                FileSize = userFileManager.FileSize
            };

            return true;
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        private RemoteFileInfo ClearFileManagerAndStartDownload(DownloadRequest request, IUserFileManager currentFileManager)
        {
            //Clear the File in FileMannager
            ClearFileManager(currentFileManager);

            var retAns = EnqueueWaitAndReturnCurrentLine(TaskType.Download, request, new RemoteFileInfo());
            //Check if Error happended
            if (retAns != "Buffering")
            {
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Download File: {0}", retAns),
                    PathToSaveOnServer = ""
                };
            }
            else
            {
                var fileSize = currentFileManager.FileStream != null ? double.Parse(currentFileManager.FileSize) : 0;
                var precent = currentFileManager.FileStream != null ? ((currentFileManager.FileStream.Position / fileSize) * 100) : 0;
                precent = precent > 100 ? 100 : precent;
                var messagePrecent = string.Format("Buffering File in Server Memory {0} %", (long)precent);
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = messagePrecent,
                    PathToSaveOnServer = ""
                };
            }
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        private void ClearFileManager(IUserFileManager currentFileManager)
        {
            if (currentFileManager.FileStream != null)
            {
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;
            }
            try
            {
                var path = currentFileManager.Path;
                if (fileHelper.Exists(path))
                    fileHelper.Delete(path);
            }
            catch { }
        }

        public RemoteFileInfo ActiveDownloadFile(DownloadRequest request)
        {
            if (CheckIfDownloadErrorAndReturn(out var errorResult)) return errorResult;

            if (CheckIfWeStillBufferingAndReturn(request, out var bufferingPrecentResult)) return bufferingPrecentResult;

            if (CheckWeStillDownloadingAndHaveChunksToRead(request, out var finalChunk)) return finalChunk;

            if (CheckWeHaveDownloadInProcessAndSendChunk(request, out var chunk)) return chunk;
            //there is no download in process, so get files from passive client
            return ClearFileManagerAndStartDownload(request, userFileManager);
        }

        private bool CheckIfUploadErrorAndReturn(out RemoteFileInfo result)
        {
            result = null;
            if (!userFileManager.Error) return false;

            userFileManager.Error = false;
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = string.Format("Error Upload File: {0}", userFileManager.ErrorMessage),
                PathToSaveOnServer = ""
            };
            return true;
        }

        private bool CheckIfUploadEndedAndReturn(out RemoteFileInfo result)
        {
            result = null;
            if (!userFileManager.UploadingEnded) return false;

            userFileManager.UploadingEnded = false;
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = "Upload Ended",
                PathToSaveOnServer = ""
            };
            return true;
        }

        private bool CheckIfBufferingInRemoteClientAndReturn(out RemoteFileInfo result)
        {
            result = null;
            if (!userFileManager.Buffering) return false;

            var fileSize = double.Parse(userFileManager.FileSize);
            fileSize = fileSize == 0 ? 1 : fileSize;
            var precent = (userFileManager.ReadSoFar / fileSize) * 100;
            precent = precent > 100 ? 100 : precent;
            var messagePrecent = string.Format("Buffering File in passive client Memory {0} %", (long)precent);
            result = new RemoteFileInfo()
            {
                FileByteStream = new byte[0],
                FileName = messagePrecent,
                PathToSaveOnServer = ""
            };
            return true;
        }

        private void DeletePrevTmpFileAndCreateNewTmpFileForUpload(RemoteFileInfo request)
        {
            if (request.FreshStart && userFileManager.FileStream != null)
            {
                userFileManager.FileStream.Close();
                userFileManager.FileStream = null;
                try
                {
                    var path = userFileManager.Path;
                    fileHelper.Delete(path);
                }
                catch { }
            }

            if (userFileManager.FileStream != null) return;

            var newGuid = Guid.NewGuid();
            userFileManager.Path = Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString(), request.FileName);
            if (!directoryHelper.Exists(Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString())))
                directoryHelper.CreateDirectory(Path.Combine(directoryHelper.GetTempPath(), newGuid.ToString()));
            userFileManager.FileStream = fileHelper.GetFileStream(userFileManager.Path,
                                                           FileMode.Create,
                                                           FileAccess.ReadWrite,
                                                           FileShare.ReadWrite);

            userFileManager.FileSize = request.FileSize;

        }

        private bool CheckIfUploadNotEndedAndReturn(RemoteFileInfo request, out RemoteFileInfo result)
        {
            result = null;
            //When the passive client finish sending all the chumks he will signal with the request.FileEnded flag
            if (!request.FileEnded)
            {
                userFileManager.FileStream.Write(request.FileByteStream, 0, request.FileByteStream.Length);
            }
            else
            {
                //download from pasive client ended. now we need to notify the active client
                userFileManager.FileSize = userFileManager.FileStream.Length.ToString();
                userFileManager.FileStream.Close();
                userFileManager.FileStream = null;
            }
            //If the still geting bytes from the active client
            if (!request.FileEnded)
            {
                result = new RemoteFileInfo() { FileByteStream = new byte[0] };
                return true;
            }

            return false;
        }

        private RemoteFileInfo EnqueueInClientQueueAndReturnResult(RemoteFileInfo request)
        {
            var retAns = EnqueueWaitAndReturnCurrentLine(TaskType.Upload, new DownloadRequest(), request);

            if (retAns != "Buffering")
            {
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Download File: {0}", retAns),
                    PathToSaveOnServer = ""
                };
            }
            else
            {
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = "Buffering File in passive client Memory 0 %",
                    PathToSaveOnServer = ""
                };
            }
        }

        public RemoteFileInfo ActiveUploadFile(RemoteFileInfo request)
        {
            if (CheckIfUploadErrorAndReturn(out var error)) return error;

            if (CheckIfUploadEndedAndReturn(out var uploadEnded)) return uploadEnded;

            if (CheckIfBufferingInRemoteClientAndReturn(out var bufferingPrecentInfo)) return bufferingPrecentInfo;

            DeletePrevTmpFileAndCreateNewTmpFileForUpload(request);

            if (CheckIfUploadNotEndedAndReturn(request, out var keepBuffering)) return keepBuffering;

            return EnqueueInClientQueueAndReturnResult(request);
        }

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private string EnqueueWaitAndReturnCurrentLine(TaskType command, DownloadRequest downloadRequest, RemoteFileInfo uploadRequest)
        {
            var currentUserCallbacks = userClientManager.CallBacks;
            var currentUserTransferQueue = userTaskQueue.TransferTaskQueue;

            object retRequest = null;
            var currentSelectedClient = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            atomicOperation.PerformAsAtomicFunction(() =>
            {
                selectedClient = currentSelectedClient = userClientManager.SelectedClient;
                if (selectedClient == null || !currentUserCallbacks.ContainsKey(selectedClient))
                {
                    retRequest = "Client does not exsits";
                    return;
                }
                taskId = Guid.NewGuid().ToString();
                if (downloadRequest != null)
                    downloadRequest.taskId = taskId;
                if (uploadRequest != null)
                    uploadRequest.taskId = taskId;
                currentUserTransferQueue[userClientManager.SelectedClient].Enqueue(
                    new TransferTask(command, downloadRequest, uploadRequest,
                (obj) =>
                {
                    retRequest = obj;
                    if (obj is string)
                    {
                        userFileManager.Error = true;
                        userFileManager.ErrorMessage = (string)obj;
                    }
                    else
                    {
                        userFileManager.IsDownloding = true;
                        userFileManager.UploadingEnded = true;
                        userFileManager.IsUploading = false;
                    }
                    userFileManager.Buffering = false;
                    userFileManager.ReadSoFar = 0;
                }));
                clientCallBack = currentUserCallbacks[selectedClient];
                userFileManager.Buffering = true;
                userFileManager.IsUploading = true;
                userFileManager.UploadingEnded = false;
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
            {
                if (!(retRequest is string))
                    retRequest = "Error: Client CallBack is Not Found";
            }
            return retRequest != null ? (string)retRequest : "Buffering";
        }
    }
}
