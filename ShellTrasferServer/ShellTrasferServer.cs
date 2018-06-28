using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Data;
using System.IO;
using ShellTrasferServer;
using System.Threading;
using System.Net.Http;
using System.Collections.Concurrent;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;
using WcfLogger;
using PostSharp.Patterns.Diagnostics;

[assembly: Log]
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace ShellTrasferServer
{
    [LoggingBehavior]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class ShellTransfer : IActiveShell, IPassiveShell , IAletCallBack , IRestService , IActiveShellPassiveshell
    {
        string Version = "11";

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

        bool IActiveShell.DeleteClientTask(string id, bool shellTask, int taksNumber)
        {
            return DeleteClientTask(id, shellTask, taksNumber);
        }

        public static bool DeleteClientTask(string id, bool shellTask, int taksNumber,bool safeToPassLock = false)
        {
            return AtomicOperation.PerformAsAtomicFunction<bool>(() =>
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
                        return true;
                    }
                }
            return false;
            }, safeToPassLock);
        }

       public bool ClearAllData(string id)
        {
          return AtomicOperation.PerformAsAtomicFunction<bool>(() =>
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
            return AtomicOperation.PerformAsAtomicFunction<bool>(() =>
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
            return AtomicOperation.PerformAsAtomicFunction<bool>(() =>
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
            return AtomicOperation.PerformAsAtomicFunction<string>(() =>
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
            AtomicOperation.PerformAsAtomicFunction(() =>
            { 
                foreach (var client in TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, true, 1,true);
                }
                TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue.Clear();
                foreach (var client in TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, false, 1,true);
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
                var messagePrecent = string.Format("Buffering File in Server Memory {0} %", (long)precent);
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = messagePrecent,
                    PathToSaveOnServer = ""
                };
            }
            //Check if we We have still download in process and we still have chunks to read
            if (!request.NewStart  && !(currentFileManager.IsDownloding && currentFileManager.EnumerableChunk.MoveNext()))
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
            if(currentFileManager.FileStream != null)
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
                var fileSize = currentFileManager.FileStream != null ?  double.Parse(currentFileManager.FileSize) : 0;
                var precent = currentFileManager.FileStream != null ? ((currentFileManager.FileStream.Position / fileSize) * 100) : 0;
                var messagePrecent = string.Format("Buffering File in Server Memory {0} %", (long)precent);
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = messagePrecent,
                    PathToSaveOnServer = ""
                };
            }
        }
        
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

                if(request.FreshStart && currentFileManager.FileStream != null)
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

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private static string EnqueueWaitAndReturnBaseLine(string command, string args)
        {
            var currentUserCallbacks = ClientManager.Instance.CurretUserClientManager.CallBacks;
            var currentUserManager = ClientManager.Instance.CurretUserClientManager;
            var shellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;
            var deletedTasks = ClientManager.Instance.CurretUserClientManager.DeletedTasks;

            var clientNotExcecuetedCommand = true;
            var currentSelectedClient = string.Empty;
            var retBaseLine = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            AtomicOperation.PerformAsAtomicFunction(() =>
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
                shellQueue[currentUserManager.SelectedClient].Enqueue( new ShellTask(command, args,
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
                catch(Exception e)
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
        private static string EnqueueWaitAndReturnBaseLine(TaskType command, DownloadRequest downloadRequest, RemoteFileInfo uploadRequest)
        {
            var currentUserManager = ClientManager.Instance.CurretUserClientManager;
            var currentUserFileManager = FileMannager.Instance.CurrentUserFileMannager;
            var currentUserCallbacks = currentUserManager.CallBacks;
            var currentUserTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;

            object retRequest = null;
            var currentSelectedClient = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            AtomicOperation.PerformAsAtomicFunction(() => 
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
                if(!(retRequest is string))
                    retRequest =  "Error: Client CallBack is Not Found";
            }
            return retRequest != null ? (string) retRequest : "Buffering";
        }
        #endregion IActiveShell Implemantations

        #region IPassiveShell Implemantations

        public bool PassiveClientRun(string id,string taskId, string baseLine)
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
                return new Tuple<string, string,string>(closeShell, "","");
            }
            //In case that between the has command and the passive client get command the task has been deleted
            if (currentShellQueue[id].Count == 0)
                return new Tuple<string, string, string>("", "", "");

            var task = currentShellQueue[id].Peek();
            return new Tuple<string, string,string>(task.Command, task.Args,task.TaskId);
        }

        public void CommandResponse(string id,string taskId, string baseLine)
        {
            var currentUserDeletedTasks = ClientManager.Instance.CurretUserClientManager.Deleted;
            var currentShellQueue = TaskQueue.Instance.CurrentUserTaskQueue.ShellQueue;


            if (baseLine == "CleanId")
            {
                //unique case that the passive client got some exception and he want to start over, so he delete his
                //previous id and start fresh 
                
                //This is not part of ActiveClient command process. this was promoted by the passiveClient so there is
                //no conflict in the locks.

                //prevent the active user using the Selected Client or the CallBack Dictionary when in the mean time 
                //is been deleted by the passive client func. Let the first caller finish his operation and then perform
                //the second call
                AtomicOperation.PerformAsAtomicFunction(() =>
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

        public RemoteFileInfo PassiveGetUploadFile(DownloadRequest id)
        {
            var currentTransferQueue = TaskQueue.Instance.CurrentUserTaskQueue.TransferTaskQueue;
            var currentFileManager = FileMannager.Instance.CurrentUserFileMannager;

            //In case that between the has command and the passive client get command the task has been deleted
            if (!currentTransferQueue.ContainsKey(id.id) || !currentTransferQueue[id.id].Any())
            {
                return new RemoteFileInfo()
                {
                    taskId = Guid.Empty.ToString(),
                    FileName = string.Empty,
                    FileByteStream = new byte[0],
                    FileEnded = true ,
                    id = id.id,
                    FileSize = currentFileManager.FileSize
                };
            }

            var uploadTask = currentTransferQueue[id.id].Peek();
            var req = uploadTask.RemoteFileInfo;

            //We dont want pices of byte in the wrong order
            return AtomicOperation.PerformAsAtomicUpload<RemoteFileInfo>(() =>
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

        public void PassiveUploadedFile(string id,string taskId)
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

        public void ErrorUploadDownload(string id,string taskId, string response)
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

        void IPassiveShell.ErrorNextCommand(string id,string taskId, string response)
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
            if (version != Version)
                return false;
            return AtomicOperation.PerformAsAtomicSubscribe(() =>
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

        #region IActiveShellPassiveshell Implementations

        //Waits that the server finish handle the request, if not there will be to many connecions wating 
        //then we get a timeout exception
        public bool IsTransferingData()
        {
            return AtomicOperation.isTransferingData;
        }

        public void StartTransferData()
        {
            AtomicOperation.isTransferingData = true;
        }

        #endregion IActiveShellPassiveshell Implementations

        #region Server Private functions

        private bool AllowOnlySelectedClient(string id)
        {
            return ClientManager.Instance.CurretUserClientManager.SelectedClient == id;
        }

        private void DefineSelectedClient(string id)
        {
            ClientManager.Instance.CurretUserClientManager.SelectedClient = id;
        }

        private static void RemoveClient(string id,bool onlyFromServer = false)
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
            currentUserStatusCallbacks.TryRemove(id,out iCallBackObj);
            currentClientManager.NickNames.Remove(id);
            Queue<ShellTask> shelElement;
            shelElement = currentShellQueue[id];
            if(shelElement != null)
                foreach(var element in shelElement)
                {
                    //In all removeClient invokes its in AtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, true, 1,true);
                }
            currentShellQueue.TryRemove(id, out shelElement);
            Queue<TransferTask> transferElement;
            transferElement = currentTransferQueue[id];
            if (transferElement != null)
                foreach (var element in transferElement)
                {
                    //In all removeClient invokes its in AtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, false, 1,true);
                }
            currentTransferQueue.TryRemove(id, out transferElement);
            currentUserDeleted.Add(id);
            try
            {
                if(!onlyFromServer)
                {
                    clientCallBack.CallBackFunction(id);
                    //Must do 2 times, the first not doing anything
                   // clientCallBack.CallBackFunction(id);
                }
                   
            }
            catch { }
            finally
            {
                if (currentUserCallbacks.Count > 0 && id== selectedClient)
                {
                    currentClientManager.SelectedClient = currentUserCallbacks.First().Key;
                }
                else if(currentUserCallbacks.Count == 0 && id == selectedClient)
                {
                    currentClientManager.SelectedClient = null;
                }
            }
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

        #endregion Server Private functions

        #region ICallBack Implementation

        //@pre : ClientManager.Instance.UserToUserClientManager.CallBacks.contains(id)
        public void RegisterCallBackFunction(string id, string type)
        {
            if (type == "status")
            {
                
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.CurretUserClientManager.StatusCallBacks[id] = callback;
                }
                catch
                {} 
            }
            else
            {
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.CurretUserClientManager.CallBacks[id] = callback;
                }
                catch
                {}
            }
        }

        public void KeepCallBackAlive(string id)
        {
            try
            {
                //Send data through the connection pipe
                if (ClientManager.Instance.CurretUserClientManager.CallBacks.ContainsKey(id))
                    ClientManager.Instance.CurretUserClientManager.CallBacks[id].KeepCallbackALive();
            }
            catch {} 
        }
        #endregion ICallBack Implementation

        #region REST Implementation

        public Stream RestGetStatus()
        {
            var status = GetStatus();
            status = HtmlPrintColor(status, new Dictionary<Regex, string>
             {
                                {new Regex("True") , "#00ff00"},
                                {new Regex("False"), "#ff0000"},
                                {new Regex("NickName(.*)"), "Blue"},
                                {new Regex("Client|number(.*)" ), "#ffcc66"}
                            });
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", status);
            byte[] resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        public Stream RestSelectClient(string id)
        {
            return ReturnPage(SelectClient(id),
                                    string.Format("Fail to select client {0}, <BR>try using Status command to check the client status", id),
                                    string.Format("Connected to Client id: {0}", id));
        }

        public Stream ClearAllData()
        {
            return ReturnPage(ClearAllData(string.Empty),
                                    string.Format("Fail to clear all data"),
                                    string.Format("All data cleared"));
        }

        public Stream CloseClient(string id)
        {
            return ReturnPage(ActiveCloseClient(id),
                                   string.Format("Fail to close Client id : {0}", id),
                                   string.Format("Client id : {0} successfully closed", id));
        }

        public Stream DeleteClientTask(string ClientId, string type, string number)
        {
            return ReturnPage(DeleteClientTask(ClientId, !(type.Contains("Download") || type.Contains("Upload")), int.Parse(number)),
                                 string.Format("Fail to delete task for Client id : {0}", ClientId),
                                 string.Format("Client id : {0} task of type {1} and number {2} deleted", ClientId, type, number));
        }

        public Stream SetNickName(string ClientId, string nickName)
        {
            return ReturnPage(ActiveSetNickName(ClientId, nickName),
                                string.Format("Error set nickName for client id: {0}", ClientId),
                                string.Format("Nick Name Updated for client id: {0}", ClientId));     
        }

        public Stream Help()
        {
            var helpStr = new StringBuilder();
            helpStr.AppendLine("1. /Status - Gets the curent clients status as the server");
            helpStr.AppendLine("2. /SelectClient {id} - Selects the specified client");
            helpStr.AppendLine("3. /ClearAllData - Delete all the server data");
            helpStr.AppendLine("4. /CloseClient {id} - Delete the client server data and sends close message to the client");
            helpStr.AppendLine("5. /DeleteClientTask {id} {type} {number} - Delete the client id task of type Upload\\Download\\Shell which is {number} in the status task data");
            helpStr.AppendLine("6. /SetNickName {id} {nick} - Adds nick to the specified client");
            helpStr.AppendLine("7. /Run - open cmd at the selected client");
            helpStr.AppendLine("8. /NextCommand {command}- performs the command at the cmd");
            helpStr.AppendLine("");
            helpStr.AppendLine("Notes:");
            helpStr.AppendLine("1. The syntax for the \\ seprator is |||. So in order to type c:\\ just type c:|||");


            var help = HtmlPrintColor(helpStr.ToString(), new Dictionary<Regex, string>
             {
                                {new Regex("[0-9]."), "Blue"},
                                {new Regex("/(.*)"), "Green"},
                                {new Regex("Notes:"), "Red"},

                            });
            var response = new HttpResponseMessage();
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", help);
            byte[] resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        public Stream Run()
        {
            var response = ActiveClientRun();
            response = response.Replace("\r", "");
            response = Regex.Replace(response, "[<>]", "");
            response = response.Replace("\n", "<BR>");
            return ReturnPage(true,
                                string.Empty,
                                response);
        }

        public Stream NextCommand(string command)
        {
            //No sucsess sending the seperator \ in the string
            command = command.Replace("|||", "\\");
            var response = ActiveNextCommand(command);
            response = Regex.Replace(response, "[<>]", "");
            response = response.Replace("\r", "");
            response = response.Replace("\n", "<BR>");
            return ReturnPage(true,
                                string.Empty,
                                response);
            
        }

        private Stream ReturnPage(bool success, string errorMessage, string successMessage)
        {
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", success ? successMessage : errorMessage);
            var resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        private static string HtmlPrintColor(string text, Dictionary<Regex,string> wordsToColor)
        {
            var str = new StringBuilder();
            var replace = text.Replace("\r", "");
            replace = replace.Replace("\t", "         ");
            var withNoNewLine = replace.Split('\n');
            foreach (var line in withNoNewLine)
            {
                var words = line.Split(' ');
                foreach (var firstWord in words)
                {
                    if (wordsToColor.Count(key => key.Key.IsMatch(firstWord)) == 1)
                    {
                        str.AppendFormat("<font color=\"{0}\">{1}</font>", wordsToColor.First(key => key.Key.IsMatch(firstWord)).Value, firstWord);
                    }
                    else
                    {
                        str.Append(firstWord);
                    }
                    str.Append(" ");

                }
                str.Append("<BR>");
            }
            return str.ToString();
        }
        #endregion REST Implementation



    }
}


namespace Data
{


    public class TaskQueue
    {
        private static volatile TaskQueue instance;
        private static object syncRoot = new Object();
        private TaskQueue() { }

        private ConcurrentDictionary<string, UserTaskQueue> _userToUserTaskQueue = new ConcurrentDictionary<string, UserTaskQueue>();
        public UserTaskQueue CurrentUserTaskQueue
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserTaskQueue.ContainsKey(activeUserId))
                {
                    _userToUserTaskQueue[activeUserId] = new UserTaskQueue();
                }
                return _userToUserTaskQueue[activeUserId];
            }
        }

        public static TaskQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TaskQueue();
                    }
                }

                return instance;
            }
        }

    }

    public class ClientManager
    {

        private static volatile ClientManager instance;
        private static object syncRoot = new Object();
        private ClientManager() { }

        private ConcurrentDictionary<string, UserClientManager> _userToUserClientManager = new ConcurrentDictionary<string, UserClientManager>();
        public UserClientManager CurretUserClientManager
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserClientManager.ContainsKey(activeUserId))
                    _userToUserClientManager[activeUserId] = new UserClientManager();
                return _userToUserClientManager[activeUserId];
            }
        }

        public static ClientManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new ClientManager();
                    }
                }

                return instance;
            }
        }

    }

    public static class AtomicOperation
    {
        private static object AtomicOperationLock = new Object();
        private static object AtomicSubscribeLock = new Object();
        private static object AtomicDownloadLock = new Object();
        private static object AtomicUploadLock = new Object();
        public static bool isTransferingData = false;
        private static T PerformAsAtomicFunction<T>(Func<T> func, object lockobj, bool safeToPassLock = false)
        {
            if (!safeToPassLock)
                Monitor.Enter(lockobj);
            T ret = default(T);
            try
            {
                ret = func();
            }
            catch { }
            if (!safeToPassLock)
                Monitor.Exit(lockobj);
            return ret;
        }
        public static T PerformAsAtomicFunction<T>(Func<T> func, bool safeToPassLock = false)
        {
            return PerformAsAtomicFunction<T>(func, AtomicOperationLock);
        }
        public static void PerformAsAtomicFunction(Action func, bool safeToPassLock = false)
        {
            if(!safeToPassLock)
                Monitor.Enter(AtomicOperationLock);
            try
            {
                func();
            }
            catch { }
            if (!safeToPassLock)
                Monitor.Exit(AtomicOperationLock);
        }
        public static void PerformAsAtomicDownload(Action func)
        {

            Monitor.Enter(AtomicDownloadLock);
            try
            {
                func();
            }
            catch { }
            Monitor.Exit(AtomicDownloadLock);
            isTransferingData = false;

        }

        public static T PerformAsAtomicUpload<T>(Func<T> func)
        {
            return PerformAsAtomicFunction<T>(func, AtomicUploadLock);
        }

        public static bool PerformAsAtomicSubscribe(Func<bool> func)
        {
            return PerformAsAtomicFunction<bool>(func, AtomicSubscribeLock);
        }
    }

    public class FileMannager
    {
        private static object syncRoot = new Object();
        private static volatile FileMannager instance;
        private FileMannager() { }


        private ConcurrentDictionary<string, UserFileManager> _userToUserFileMannager = new ConcurrentDictionary<string, UserFileManager>();
        public UserFileManager CurrentUserFileMannager
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserFileMannager.ContainsKey(activeUserId))
                    _userToUserFileMannager[activeUserId] = new UserFileManager();
                return _userToUserFileMannager[activeUserId];
            }
        }

        public static FileMannager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FileMannager();
                    }
                }

                return instance;
            }
        }

    }

}
