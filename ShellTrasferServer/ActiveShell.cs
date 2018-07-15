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
    public class ActiveShell : ActiveShellPassiveshell , IActiveShell
    {
        #region IActiveShell Implemantations
        const string run = "Run";
        const string nextCommand = "NextCommand";
        const string download = "Download";
        const string upload = "Upload";
        const string closeShell = "CloseShell";

        public bool ActiveSetNickName(string id, string nickName)
        {
            var temp = ClientManager.Instance.CurretUserClientManager.NickNames[id];
            var curretUserClientManager = ClientManager.Instance.CurretUserClientManager;
            try
            {
                var curretUserCallbacks = curretUserClientManager.CallBacks[id];
                curretUserClientManager.NickNames[id] = nickName;
                curretUserCallbacks.CallBackFunction(string.Format("nickName : {0}", nickName));
                return true;
            }
            catch
            {
                curretUserClientManager.NickNames[id] = temp;
                return false;
            }
        }

        public bool DeleteClientTask(string id, bool shellTask, int taksNumber)
        {
            return DeleteClientTask(id, shellTask, taksNumber);
        }

        public bool ClearAllData(string id)
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            return currentUserAtomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                ClientManager.Instance.CurretUserClientManager.Deleted.Clear();
                ClientManager.Instance.CurretUserClientManager.SelectedClient = null;
                foreach (var client in ClientManager.Instance.CurretUserClientManager.CallBacks.Keys)
                {
                    RemoveClient(client);
                }
                ClientManager.Instance.CurretUserClientManager.NickNames.Clear();
                ClientManager.Instance.CurretUserClientManager.CallBacks.Clear();
                ClientManager.Instance.CurretUserClientManager.StatusCallBacks.Clear();
                TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.Clear();
                TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue.Clear();
                return true;
            });
        }

        public bool ActiveCloseClient(string id)
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            return currentUserAtomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                if (ClientManager.Instance.CurretUserClientManager.CallBacks.ContainsKey(id))
                {
                    RemoveClient(id);
                    return true;
                }
                return false;
            });
        }

        public bool SelectClient(string id)
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            return currentUserAtomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                var clients = ClientManager.Instance.CurretUserClientManager.CallBacks;
                if (!clients.ContainsKey(id))
                    return false;

                if (!IsClientAlive(id))
                {
                    RemoveClient(id);
                    return false;
                }
                //connect the slected client
                ClientManager.Instance.CurretUserClientManager.SelectedClient = id;
                return true;
            });
        }

        public string GetStatus()
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            return currentUserAtomicOperation.PerformAsAtomicFunction<string>(() =>
            {
                var clients = ClientManager.Instance.CurretUserClientManager.CallBacks.ToList();
                var status = new StringBuilder();
                var clientCounter = 1;
                status.AppendLine("The Status: ");
                if (clients.Count > 0)
                {
                    foreach (var client in clients)
                    {
                        var isAlive = IsClientAlive(client.Key);
                        var nickName = ClientManager.Instance.CurretUserClientManager.NickNames[client.Key];
                        status.AppendLine(string.Format("Client number{0} id: {1}\tNickName:{3}\nIs Alive: {2}"
                                                          , clientCounter, client.Key, isAlive, nickName));
                        var taskCounter = 1;
                        if (TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.ContainsKey(client.Key))
                        {
                            status.AppendLine("Shell Tasks:");
                            var clientTasks = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue[client.Key].ToList();
                            if (clientTasks.Count > 0)
                            {
                                foreach (var task in clientTasks)
                                {
                                    status.AppendLine(string.Format("Task Number: {0}", taskCounter));
                                    status.AppendLine(string.Format("{0} {1}", task.Command, task.Args));
                                    taskCounter++;
                                }
                            }
                            else
                            {
                                status.AppendLine("There is no shell tasks");
                            }
                        }
                        if (TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue.ContainsKey(client.Key))
                        {
                            taskCounter = 1;
                            status.AppendLine("Upload And Download Tasks:");
                            var clientTasks = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue[client.Key].ToList();
                            if (clientTasks.Count > 0)
                            {
                                foreach (var task in clientTasks)
                                {
                                    status.AppendLine(string.Format("Task Number: {0}", taskCounter));

                                    if (task.TaskType == TaskType.Upload)
                                    {
                                        status.AppendLine(string.Format("{0} {1} {2}",
                                                                         task.TaskType,
                                                                         task.RemoteFileInfo.FileName,
                                                                         task.RemoteFileInfo.PathToSaveOnServer));
                                    }
                                    if (task.TaskType == TaskType.Download)
                                    {
                                        status.AppendLine(string.Format("{0} {1} {2} {3}",
                                                                        task.TaskType,
                                                                        task.DownloadRequest.FileName,
                                                                        task.DownloadRequest.PathInServer,
                                                                        task.DownloadRequest.PathToSaveInClient));
                                    }

                                    taskCounter++;
                                }
                            }
                            else
                            {
                                status.AppendLine("There is no Download or Upload tasks");
                            }
                        }
                        status.AppendLine(string.Format(""));
                        /*If the passiveClient computer is off the we dont want to delete the client. we want to wait 
                         * until his compueter be on again
                        if (!isAlive)
                        {
                            RemoveClient(client.Key);
                            status.AppendLine(string.Format("Removed {0} from Client list", client.Key));
                        }
                        */
                        clientCounter++;
                    }
                    status.AppendLine(string.Format("The selected Client id is: {0}",
                        ClientManager.Instance.CurretUserClientManager.SelectedClient));
                }
                else
                {
                    status.AppendLine(string.Format("There is no clients connected"));
                }
                return status.ToString();
            });
        }

        public void ClearQueue()
        {
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            currentUserAtomicOperation.PerformAsAtomicFunction(() =>
            {
                foreach (var client in TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, true, 1, true);
                }
                TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.Clear();
                foreach (var client in TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, false, 1, true);
                }
                TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue.Clear();
            });
        }

        public string ActiveNextCommand(string args)
        {
            return EnqueueWaitAndReturnBaseLine(nextCommand, args);
        }

        public string ActiveClientRun()
        {
            return EnqueueWaitAndReturnBaseLine(run, "");
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public RemoteFileInfo ActiveDownloadFile(DownloadRequest request)
        {
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;
            if (currentFileManager.Error)
            {
                currentFileManager.Error = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Download File: {0}", currentFileManager.ErrorMessage),
                    PathToSaveOnServer = ""
                };
            }
            //Check if We still buffering in server
            if (!request.NewStart && currentFileManager.Buffering)
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
            //Check if we We have still download in process and we still have chunks to read
            if (!request.NewStart && !(currentFileManager.IsDownloding && currentFileManager.EnumerableChunk.MoveNext()))
            {
                try
                {
                    var path = currentFileManager.Path;
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch { }
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = request.FileName,
                    PathToSaveOnServer = request.PathToSaveInClient,
                    FileEnded = true
                };
            }
            //We have still download in process and we have chunks to write
            if (!request.NewStart && currentFileManager.IsDownloding)
            {
                return new RemoteFileInfo()
                {
                    FileByteStream = currentFileManager.EnumerableChunk.Current.Item1,
                    FileName = request.FileName,
                    PathToSaveOnServer = string.Empty,
                    FileEnded = false,
                    Length = currentFileManager.EnumerableChunk.Current.Item2,
                    FileSize = currentFileManager.FileSize
                };
            }
            //there is no download in process, so get files from passive client
            //Clear the File in FileMannager
            if (currentFileManager.FileStream != null)
            {
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;
            }
            try
            {
                var path = currentFileManager.Path;
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
            var retAns = EnqueueWaitAndReturnBaseLine(TaskType.Download, request, new RemoteFileInfo());
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
        public RemoteFileInfo ActiveUploadFile(RemoteFileInfo request)
        {
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;
            if (currentFileManager.Error)
            {
                currentFileManager.Error = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Upload File: {0}", currentFileManager.ErrorMessage),
                    PathToSaveOnServer = ""
                };
            }

            if (currentFileManager.UploadingEnded)
            {
                currentFileManager.UploadingEnded = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = "Upload Ended",
                    PathToSaveOnServer = ""
                };
            }

            if (currentFileManager.Buffering)
            {
                var fileSize = double.Parse(currentFileManager.FileSize);
                fileSize = fileSize == 0 ? 1 : fileSize;
                var precent = (currentFileManager.ReadSoFar / fileSize) * 100;
                precent = precent > 100 ? 100 : precent;
                var messagePrecent = string.Format("Buffering File in passive client Memory {0} %", (long)precent);
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = messagePrecent,
                    PathToSaveOnServer = ""
                };
            }
            //We will save all the byte chunks in temp file, its propebly will became to big with all the add actions
            //for some data strcture so we probebly will get SystemOutOfMemory exception

            if (request.FreshStart && currentFileManager.FileStream != null)
            {
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;
                try
                {
                    var path = currentFileManager.Path;
                    File.Delete(path);
                }
                catch { }
            }

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
            }
            else
            {
                //download from pasive client ended. now we need to notify the active client
                currentFileManager.FileSize = currentFileManager.FileStream.Length.ToString();
                currentFileManager.FileStream.Close();
                currentFileManager.FileStream = null;
            }
            //If the still geting bytes from the active client
            if (!request.FileEnded)
                return new RemoteFileInfo() { FileByteStream = new byte[0] };

            var retAns = EnqueueWaitAndReturnBaseLine(TaskType.Upload, new DownloadRequest(), request);

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

        #endregion IActiveShell Implemantations

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private string EnqueueWaitAndReturnBaseLine(string command, string args)
        {
            var currentUserCallbacks = ClientManager.Instance.CurretUserClientManager.CallBacks;
            var currentUserManager = ClientManager.Instance.CurretUserClientManager;
            var shellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            var deletedTasks = ClientManager.Instance.CurretUserClientManager.DeletedTasks;
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            var clientNotExcecuetedCommand = true;
            var currentSelectedClient = string.Empty;
            var retBaseLine = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            currentUserAtomicOperation.PerformAsAtomicFunction(() =>
            {
                selectedClient = currentUserManager.SelectedClient;
                if (selectedClient == null || !currentUserCallbacks.ContainsKey(selectedClient))
                {
                    retBaseLine = "Error: Client does not exsits";
                    clientNotExcecuetedCommand = false;
                    return;
                }
                taskId = Guid.NewGuid().ToString();
                currentSelectedClient = currentUserManager.SelectedClient;
                shellQueue[currentUserManager.SelectedClient].Enqueue(new ShellTask(command, args,
                    (str) =>
                    {
                        retBaseLine = str;
                        clientNotExcecuetedCommand = false;
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
            while (clientNotExcecuetedCommand)
            {
                if (deletedTasks.Contains(taskId))
                {
                    clientNotExcecuetedCommand = false;
                    retBaseLine = "Task were deleted";
                }
            }
            return retBaseLine;
        }

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private string EnqueueWaitAndReturnBaseLine(TaskType command, DownloadRequest downloadRequest, RemoteFileInfo uploadRequest)
        {
            var currentUserManager = ClientManager.Instance.CurretUserClientManager;
            var currentUserFileManager = FileMannager.Instance.CurrentUserFileMannager;
            var currentUserCallbacks = currentUserManager.CallBacks;
            var currentUserTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentUserAtomicOperation = UserAtomicOperation.Instance.AtomicOperation;

            object retRequest = null;
            var currentSelectedClient = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            currentUserAtomicOperation.PerformAsAtomicFunction(() =>
            {
                selectedClient = currentSelectedClient = currentUserManager.SelectedClient;
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
                currentUserTransferQueue[currentUserManager.SelectedClient].Enqueue(
                    new TransferTask(command, downloadRequest, uploadRequest,
                (obj) =>
                {
                    retRequest = obj;
                    if (obj is string)
                    {
                        currentUserFileManager.Error = true;
                        currentUserFileManager.ErrorMessage = (string)obj;
                    }
                    else
                    {
                        currentUserFileManager.IsDownloding = true;
                        currentUserFileManager.UploadingEnded = true;
                        currentUserFileManager.IsUploading = false;
                    }
                    currentUserFileManager.Buffering = false;
                    currentUserFileManager.ReadSoFar = 0;
                }));
                clientCallBack = currentUserCallbacks[selectedClient];
                currentUserFileManager.Buffering = true;
                currentUserFileManager.IsUploading = true;
                currentUserFileManager.UploadingEnded = false;
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

        private bool IsClientAlive(string id)
        {

            var currentClientManager = ClientManager.Instance.CurretUserClientManager;
            var currentUserCallbacks = currentClientManager.CallBacks;
            var currentUserStatusCallbacks = currentClientManager.StatusCallBacks;

            if (id == null || !currentUserCallbacks.ContainsKey(id))
            {
                return false;
            }
            try
            {
                var clientCallBack = currentUserStatusCallbacks[id];
                clientCallBack.CallBackFunction("livnessCheck");
                //Must check 2 times, first time go well also if the passiveClient is disconnected
                clientCallBack.CallBackFunction("livnessCheck");

                //Keep the connection with the taks callback alive 
                clientCallBack = currentUserCallbacks[id];
                clientCallBack.CallBackFunction("livnessCheck");
                //Must check 2 times, first time go well also if the passiveClient is disconnected
                clientCallBack.CallBackFunction("livnessCheck");

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

   
