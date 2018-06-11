using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Data;
using System.IO;
using ShellTrasferServer;
using System.ServiceModel.Channels;
using System.Threading;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace ShellTrasferServer
{
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
            var temp = ClientManager.Instance.NickNames[id];
            try
            {
                ClientManager.Instance.NickNames[id] = nickName;
                ClientManager.Instance.CallBacks[id].CallBackFunction(string.Format("nickName : {0}", nickName));
                return true;
            }
            catch
            {
                ClientManager.Instance.NickNames[id] = temp;
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
            if(shellTask)
            {
                if (TaskQueue.Instance.ShellQueue.ContainsKey(id) && TaskQueue.Instance.ShellQueue[id].Count > taksNumber - 1)
                {
                    var deleted = TaskQueue.Instance.ShellQueue[id].ElementAt(taksNumber - 1);
                    TaskQueue.Instance.ShellQueue[id] = TaskQueue.Instance.ShellQueue[id].DeleteAt(taksNumber - 1);
                    ClientManager.Instance.DeletedTasks.Add(deleted.Item4);
                    return true;
                }
            }
            else
            {
                if (TaskQueue.Instance.TransferTaskQueue.ContainsKey(id) && TaskQueue.Instance.TransferTaskQueue[id].Count > taksNumber - 1)
                {
                    var deleted = TaskQueue.Instance.TransferTaskQueue[id].ElementAt(taksNumber - 1);
                    TaskQueue.Instance.TransferTaskQueue[id] = TaskQueue.Instance.TransferTaskQueue[id].DeleteAt(taksNumber - 1);
                    //This is a hash set so it wont be added twice
                    ClientManager.Instance.DeletedTasks.Add(deleted.Item2.taskId);
                    ClientManager.Instance.DeletedTasks.Add(deleted.Item3.taskId);
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
                ClientManager.Instance.Deleted.Clear();
                ClientManager.Instance.SelectedClient = null;
                foreach (var client in ClientManager.Instance.CallBacks.Keys)
                {
                    RemoveClient(client);
                }
                ClientManager.Instance.NickNames.Clear();
                ClientManager.Instance.CallBacks.Clear();
                ClientManager.Instance.StatusCallBacks.Clear();
                TaskQueue.Instance.ShellQueue.Clear();
                TaskQueue.Instance.TransferTaskQueue.Clear();
                return true;
            });
        }

        public bool ActiveCloseClient(string id)
        {
            return AtomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                if (ClientManager.Instance.CallBacks.ContainsKey(id))
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
                var clients = ClientManager.Instance.CallBacks;
                if (!clients.ContainsKey(id))
                    return false;

                if (!IsClientAlive(id))
                {
                    RemoveClient(id);
                    return false;
                }
                //connect the slected client
                ClientManager.Instance.SelectedClient = id;
                return true;
            });
        }

        public string GetStatus()
        {
            return AtomicOperation.PerformAsAtomicFunction<string>(() =>
            {
                var clients = ClientManager.Instance.CallBacks.ToList();
                var status = new StringBuilder();
                var clientCounter = 1;
                status.AppendLine("The Status: ");
                if (clients.Count > 0)
                {
                    foreach (var client in clients)
                    {
                        var isAlive = IsClientAlive(client.Key);
                        var nickName = ClientManager.Instance.NickNames[client.Key];
                        status.AppendLine(string.Format("Client number{0} id: {1}\tNickName:{3}\nIs Alive: {2}"
                                                          , clientCounter, client.Key, isAlive, nickName));
                        var taskCounter = 1;
                        if (TaskQueue.Instance.ShellQueue.ContainsKey(client.Key))
                        {
                            status.AppendLine("Shell Tasks:");
                            var clientTasks = TaskQueue.Instance.ShellQueue[client.Key].ToList();
                            if (clientTasks.Count > 0)
                            {
                                foreach (var task in clientTasks)
                                {
                                    status.AppendLine(string.Format("Task Number: {0}", taskCounter));
                                    status.AppendLine(string.Format("{0} {1}", task.Item1, task.Item2));
                                    taskCounter++;
                                }
                            }
                            else
                            {
                                status.AppendLine("There is no shell tasks");
                            }
                        }
                        if (TaskQueue.Instance.TransferTaskQueue.ContainsKey(client.Key))
                        {
                            taskCounter = 1;
                            status.AppendLine("Upload And Download Tasks:");
                            var clientTasks = TaskQueue.Instance.TransferTaskQueue[client.Key].ToList();
                            if (clientTasks.Count > 0)
                            {
                                foreach (var task in clientTasks)
                                {
                                    status.AppendLine(string.Format("Task Number: {0}", taskCounter));
                                    if (task.Item1 == "Upload")
                                    {
                                        status.AppendLine(string.Format("{0} {1} {2}", task.Item1, task.Item3.FileName, task.Item3.PathToSaveOnServer));
                                    }
                                    if (task.Item1 == "Download")
                                    {
                                        status.AppendLine(string.Format("{0} {1} {2} {3}", task.Item1, task.Item2.FileName, task.Item2.PathInServer, task.Item2.PathToSaveInClient));
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
                    status.AppendLine(string.Format("The selected Client id is: {0}", ClientManager.Instance.SelectedClient));
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
                foreach (var client in TaskQueue.Instance.ShellQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, true, 1,true);
                }
                TaskQueue.Instance.ShellQueue.Clear();
                foreach (var client in TaskQueue.Instance.TransferTaskQueue.Keys)
                {
                    //Sience the function is locked we do not need the delete task lock
                    DeleteClientTask(client, false, 1,true);
                }
                TaskQueue.Instance.TransferTaskQueue.Clear();
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
            if (FileMannager.Instance.Error)
            {
                FileMannager.Instance.Error = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Download File: {0}", FileMannager.Instance.ErrorMessage),
                    PathToSaveOnServer = ""
                };
            }
            //Check if We still buffering in server
            if (!request.NewStart && FileMannager.Instance.Buffering)
            {
                var fileSize = FileMannager.Instance.FileStream != null ? double.Parse(FileMannager.Instance.FileSize) : 0;
                var precent = FileMannager.Instance.FileStream != null ?
                              ((FileMannager.Instance.FileStream.Position / fileSize) * 100) : 0;
                var messagePrecent = string.Format("Buffering File in Server Memory {0} %", (long)precent);
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = messagePrecent,
                    PathToSaveOnServer = ""
                };
            }
            //Check if we We have still download in process and we still have chunks to read
            if (!request.NewStart  && !(FileMannager.Instance.IsDownloding && FileMannager.Instance.EnumerableChunk.MoveNext()))
            {
                try
                {
                    if (File.Exists(FileMannager.Instance.Path))
                        File.Delete(FileMannager.Instance.Path);
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
            if (!request.NewStart && FileMannager.Instance.IsDownloding)
            {
                return new RemoteFileInfo()
                {
                    FileByteStream = FileMannager.Instance.EnumerableChunk.Current.Item1,
                    FileName = request.FileName,
                    PathToSaveOnServer = string.Empty,
                    FileEnded = false,
                    Length = FileMannager.Instance.EnumerableChunk.Current.Item2,
                    FileSize = FileMannager.Instance.FileSize
                };
            }
            //there is no download in process, so get files from passive client
            //Clear the File in FileMannager
            if(FileMannager.Instance.FileStream != null)
            {
                FileMannager.Instance.FileStream.Close();
                FileMannager.Instance.FileStream = null;
            }
            try
            {
                if (File.Exists(FileMannager.Instance.Path))
                    File.Delete(FileMannager.Instance.Path);
            }
            catch { }
            var retAns = EnqueueWaitAndReturnBaseLine(download, request, new RemoteFileInfo());
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
                var fileSize = FileMannager.Instance.FileStream != null ?  double.Parse(FileMannager.Instance.FileSize) : 0;
                var precent = FileMannager.Instance.FileStream != null ?
                              ((FileMannager.Instance.FileStream.Position / fileSize) * 100) : 0;
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

            if (FileMannager.Instance.Error)
            {
                FileMannager.Instance.Error = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = string.Format("Error Upload File: {0}", FileMannager.Instance.ErrorMessage),
                    PathToSaveOnServer = ""
                };
            }

            if (FileMannager.Instance.UploadingEnded)
            {
                FileMannager.Instance.UploadingEnded = false;
                return new RemoteFileInfo()
                {
                    FileByteStream = new byte[0],
                    FileName = "Upload Ended",
                    PathToSaveOnServer = ""
                };
            }

            if (FileMannager.Instance.Buffering)
            {
                var fileSize = double.Parse(FileMannager.Instance.FileSize);
                fileSize = fileSize == 0 ? 1 : fileSize;
                var precent = (FileMannager.Instance.ReadSoFar / fileSize) * 100;
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

                if(request.FreshStart && FileMannager.Instance.FileStream != null)
                {
                    FileMannager.Instance.FileStream.Close();
                    FileMannager.Instance.FileStream = null;
                try
                {
                    File.Delete(FileMannager.Instance.Path);
                }
                catch { }  
                }

            if (FileMannager.Instance.FileStream == null)
            {
                var newGuid = Guid.NewGuid();
                FileMannager.Instance.Path = Path.Combine(Path.GetTempPath(), newGuid.ToString(), request.FileName);
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), newGuid.ToString())))
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), newGuid.ToString()));
                FileMannager.Instance.FileStream = new FileStream(FileMannager.Instance.Path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileMannager.Instance.FileSize = request.FileSize;
            }
                //When the passive client finish sending all the chumks he will signal with the request.FileEnded flag
                if (!request.FileEnded)
                {
                    FileMannager.Instance.FileStream.Write(request.FileByteStream, 0, request.FileByteStream.Length);
                }
                else
                {
                    //download from pasive client ended. now we need to notify the active client
                    FileMannager.Instance.FileSize = FileMannager.Instance.FileStream.Length.ToString();
                    FileMannager.Instance.FileStream.Close();
                    FileMannager.Instance.FileStream = null;
                }
            //If the still geting bytes from the active client
            if (!request.FileEnded)
                return new RemoteFileInfo() { FileByteStream = new byte[0] };

            var retAns = EnqueueWaitAndReturnBaseLine(upload, new DownloadRequest(), request);

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
            var clientNotExcecuetedCommand = true;
            var currentSelectedClient = string.Empty;
            var retBaseLine = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            AtomicOperation.PerformAsAtomicFunction(() =>
            {
                selectedClient = ClientManager.Instance.SelectedClient;
                if (selectedClient == null || !ClientManager.Instance.CallBacks.ContainsKey(selectedClient))
                {
                    retBaseLine = "Error: Client does not exsits";
                    clientNotExcecuetedCommand = false;
                    return;
                }
                taskId = Guid.NewGuid().ToString();
                currentSelectedClient = ClientManager.Instance.SelectedClient;
                TaskQueue.Instance.ShellQueue[ClientManager.Instance.SelectedClient].Enqueue(new Tuple<string, string, Action<string>,string>(command, args,
                    (str) =>
                    {
                        retBaseLine = str;
                        clientNotExcecuetedCommand = false;
                    }, taskId));

                clientCallBack = ClientManager.Instance.CallBacks[selectedClient];
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
                if (ClientManager.Instance.DeletedTasks.Contains(taskId))
                {
                    clientNotExcecuetedCommand = false;
                    retBaseLine = "Task were deleted";
                }
            }
            return retBaseLine;
        }

        //This function should not be executed as paralel. after we got response from the passive client
        //we do not need to block the other users since the selected client preformed his action
        private static string EnqueueWaitAndReturnBaseLine(string command, DownloadRequest downloadRequest, RemoteFileInfo uploadRequest)
        {
            object retRequest = null;
            var currentSelectedClient = string.Empty;
            var taskId = string.Empty;
            var selectedClient = string.Empty;
            ICallBack clientCallBack = null;
            AtomicOperation.PerformAsAtomicFunction(() => 
            {
            selectedClient = currentSelectedClient = ClientManager.Instance.SelectedClient;
            if (selectedClient == null || !ClientManager.Instance.CallBacks.ContainsKey(selectedClient))
            {
                retRequest = "Client does not exsits";
                return;
            }
                taskId = Guid.NewGuid().ToString();
                if (downloadRequest != null)
                    downloadRequest.taskId = taskId;
                if (uploadRequest != null)
                    uploadRequest.taskId = taskId;
                TaskQueue.Instance.TransferTaskQueue[ClientManager.Instance.SelectedClient].Enqueue(
                new Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>(command, downloadRequest, uploadRequest,
                (obj) =>
                {
                    retRequest = obj;
                    if (obj is string)
                    {
                        FileMannager.Instance.Error = true;
                        FileMannager.Instance.ErrorMessage = (string)obj;
                    }
                    else
                    {
                        FileMannager.Instance.IsDownloding = true;
                        FileMannager.Instance.UploadingEnded = true;
                        FileMannager.Instance.IsUploading = false;
                    }
                    FileMannager.Instance.Buffering = false;
                    FileMannager.Instance.ReadSoFar = 0;
                }));
                clientCallBack = ClientManager.Instance.CallBacks[selectedClient];
                FileMannager.Instance.Buffering = true;
                FileMannager.Instance.IsUploading = true;
                FileMannager.Instance.UploadingEnded = false;
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
            if (!AllowOnlySelectedClient(id))
                return false;
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!TaskQueue.Instance.ShellQueue.ContainsKey(id) || !TaskQueue.Instance.ShellQueue[id].Any() ||
                TaskQueue.Instance.ShellQueue[id].Peek().Item4 != taskId)
                return false;
            var tuple = TaskQueue.Instance.ShellQueue[id].Dequeue();
            var callback = tuple.Item3;
            callback(baseLine);
            return true;
        }

        public bool HasCommand(string id)
        {
            if (ClientManager.Instance.Deleted.Contains(id))
            {
                return true;
            }
            return AllowOnlySelectedClient(id) ? TaskQueue.Instance.TransferTaskQueue.ContainsKey(id)
                && TaskQueue.Instance.ShellQueue[id].Count > 0 : false;
        }
        public Tuple<string, string, string> PassiveNextCommand(string id)
        {
            if (ClientManager.Instance.Deleted.Contains(id))
            {
                return new Tuple<string, string,string>(closeShell, "","");
            }
            //In case that between the has command and the passive client get command the task has been deleted
            if (TaskQueue.Instance.ShellQueue[id].Count == 0)
                return new Tuple<string, string, string>("", "", "");

            var task = TaskQueue.Instance.ShellQueue[id].Peek();
            return new Tuple<string, string,string>(task.Item1, task.Item2,task.Item4);
        }

        public void CommandResponse(string id,string taskId, string baseLine)
        {
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
            if (ClientManager.Instance.Deleted.Contains(id))
                return;
            //In case that between the has command and the passive client get command the task has been deleted
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!TaskQueue.Instance.ShellQueue.ContainsKey(id) || !TaskQueue.Instance.ShellQueue[id].Any() ||
                TaskQueue.Instance.ShellQueue[id].Peek().Item4 != taskId)
                return;
            var task = TaskQueue.Instance.ShellQueue[id].Dequeue();
            var callback = task.Item3;
            callback(baseLine);
        }

        public bool HasUploadCommand(string id)
        {
            return AllowOnlySelectedClient(id) ? TaskQueue.Instance.TransferTaskQueue.ContainsKey(id) 
                && TaskQueue.Instance.TransferTaskQueue[id].Count > 0 &&
                   TaskQueue.Instance.TransferTaskQueue[id].Peek().Item1 == "Upload" :
                   false;
        }

        public bool HasDownloadCommand(string id)
        {
            return AllowOnlySelectedClient(id) ? TaskQueue.Instance.TransferTaskQueue.ContainsKey(id)
                && TaskQueue.Instance.TransferTaskQueue[id].Count > 0 &&
                  TaskQueue.Instance.TransferTaskQueue[id].Peek().Item1 == "Download" :
                   false;
        }

        public DownloadRequest PassiveGetDownloadFile(DownloadRequest id)
        {
            //In case that between the has command and the passive client get command the task has been deleted
            if (!TaskQueue.Instance.TransferTaskQueue.ContainsKey(id.id) || !TaskQueue.Instance.TransferTaskQueue[id.id].Any())
            {
                return new DownloadRequest()
                {
                    taskId = Guid.Empty.ToString()
                };
            }
            var downloadTask = TaskQueue.Instance.TransferTaskQueue[id.id].Peek();
            var req = downloadTask.Item2;
            return req;
        }

        public void PassiveDownloadedFile(RemoteFileInfo request)
        {

            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!TaskQueue.Instance.TransferTaskQueue.ContainsKey(request.id) || !TaskQueue.Instance.TransferTaskQueue[request.id].Any() ||
                TaskQueue.Instance.TransferTaskQueue[request.id].Peek().Item2.taskId != request.taskId)
                return;

            //We will save all the byte chunks in temp file, its propebly will became to big with all the add actions
            //for some data strcture so we probebly will get SystemOutOfMemory exception
            if (FileMannager.Instance.FileStream == null)
            {
                var newGuid = Guid.NewGuid();
                FileMannager.Instance.Path = Path.Combine(Path.GetTempPath(), newGuid.ToString(), request.FileName);
                if (!Directory.Exists(Path.Combine(Path.GetTempPath(), newGuid.ToString())))
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), newGuid.ToString()));
                FileMannager.Instance.FileStream = new FileStream(FileMannager.Instance.Path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileMannager.Instance.FileSize = request.FileSize;
            }
            //When the passive client finish sending all the chumks he will signal with the request.FileEnded flag
            if (!request.FileEnded)
            {
                FileMannager.Instance.FileStream.Write(request.FileByteStream, 0, request.FileByteStream.Length);
                FileMannager.Instance.FileStream.Flush();
            }
            else
            {
                //download from pasive client ended. now we need to notify the active client
                FileMannager.Instance.FileStream.Close();
                FileMannager.Instance.FileStream = null;

                var uploadTask = TaskQueue.Instance.TransferTaskQueue[request.id].Dequeue();
                var callback = uploadTask.Item4;
                callback(true);
            }

        }

        public RemoteFileInfo PassiveGetUploadFile(DownloadRequest id)
        {
            //In case that between the has command and the passive client get command the task has been deleted
            if (!TaskQueue.Instance.TransferTaskQueue.ContainsKey(id.id) || !TaskQueue.Instance.TransferTaskQueue[id.id].Any())
            {
                return new RemoteFileInfo()
                {
                    taskId = Guid.Empty.ToString(),
                    FileName = string.Empty,
                    FileByteStream = new byte[0],
                    FileEnded = true ,
                    id = id.id,
                    FileSize = FileMannager.Instance.FileSize
                };
            }

            var uploadTask = TaskQueue.Instance.TransferTaskQueue[id.id].Peek();
            var req = uploadTask.Item3;

            //We dont want pices of byte in the wrong order
            return AtomicOperation.PerformAsAtomicUpload<RemoteFileInfo>(() =>
            {
               //We have still download in process and we have chunks to send
               //Must be in this order. the MoveNext() first in order to load the first chunk
               if (FileMannager.Instance.IsUploading && FileMannager.Instance.EnumerableChunk.MoveNext())
                {
                    return new RemoteFileInfo()
                    {
                        FileByteStream = FileMannager.Instance.EnumerableChunk.Current.Item1,
                        FileName = req.FileName,
                        PathToSaveOnServer = req.PathToSaveOnServer,
                        FileEnded = false,
                        Length = FileMannager.Instance.EnumerableChunk.Current.Item2,
                        taskId = req.taskId,
                        id = id.id,
                        FileSize = FileMannager.Instance.FileSize
                    };
                }

                //We dont have chunks to send so end the download 
                try
                {
                    File.Delete(FileMannager.Instance.Path);
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
                    FileSize = FileMannager.Instance.FileSize
                };
            });
        }

        public void PassiveUploadedFile(string id,string taskId)
        {
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!TaskQueue.Instance.TransferTaskQueue.ContainsKey(id) || !TaskQueue.Instance.TransferTaskQueue[id].Any() ||
                TaskQueue.Instance.TransferTaskQueue[id].Peek().Item3.taskId != taskId)
                return;
            var uploadTask = TaskQueue.Instance.TransferTaskQueue[id].Dequeue();
            var callback = uploadTask.Item4;
            callback(true);
        }

        public void ErrorUploadDownload(string id,string taskId, string response)
        {
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return

            //Delete the temp file
            if (FileMannager.Instance.FileStream != null)
            {
                FileMannager.Instance.FileStream.Close();
                FileMannager.Instance.FileStream = null;
            }
            try
            {
                if (File.Exists(FileMannager.Instance.Path))
                    File.Delete(FileMannager.Instance.Path);
            }
            catch { }
            if (!TaskQueue.Instance.TransferTaskQueue.ContainsKey(id) || !TaskQueue.Instance.TransferTaskQueue[id].Any() ||
                TaskQueue.Instance.TransferTaskQueue[id].Peek().Item3.taskId != taskId)
                return;
            var task = TaskQueue.Instance.TransferTaskQueue[id].Dequeue();
            var callback = task.Item4;
            callback(response);
        }

        void IPassiveShell.ErrorNextCommand(string id,string taskId, string response)
        {
            //When passive client been deleted, he get a signal from the callback and in hasCommand get true
            //and in NextCommand he get new mission that is not in the missionQueue so we dont need to dequeue

            //If the PassiveClient was deleted the get signal and check if he has command to execute
            //then in NextCommand he get a CloseShell command, if something go wrong the client will end up here
            //but as far as the server concern the client deleted from his data.
            if (ClientManager.Instance.Deleted.Contains(id))
            {
                return;
            }
            //It possible that the active client deleted 1 or more missions. in that case we want to ignore the callback and 
            //return
            if (!TaskQueue.Instance.ShellQueue.ContainsKey(id) || !TaskQueue.Instance.ShellQueue[id].Any() ||
                TaskQueue.Instance.ShellQueue[id].Peek().Item4 != taskId)
                return;
            var task = TaskQueue.Instance.ShellQueue[id].Dequeue();
            var callback = task.Item3;
            callback(response);
        }

        public bool Subscribe(string id, string version, string name)
        {
            if (version != Version)
                return false;
            return AtomicOperation.PerformAsAtomicSubscribe(() =>
             {
                 if (!ClientManager.Instance.CallBacks.ContainsKey(id))
                 {
                     ClientManager.Instance.CallBacks[id] = null;
                     ClientManager.Instance.NickNames[id] = string.IsNullOrWhiteSpace(name) ? "None" : name;
                     TaskQueue.Instance.ShellQueue[id] = new Queue<Tuple<string, string, Action<string>, string>>();
                     TaskQueue.Instance.TransferTaskQueue[id] = new Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>();
                     if (ClientManager.Instance.CallBacks.Count == 1)
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
            return ClientManager.Instance.SelectedClient == id;
        }

        private void DefineSelectedClient(string id)
        {
            ClientManager.Instance.SelectedClient = id;
        }

        private static void RemoveClient(string id,bool onlyFromServer = false)
        {

            if (id == null || !ClientManager.Instance.CallBacks.ContainsKey(id))
            {
                return;
            }
            var selectedClient = ClientManager.Instance.SelectedClient;
            var clientCallBack = ClientManager.Instance.CallBacks[id];
            ClientManager.Instance.CallBacks.TryRemove(id, out ICallBack iCallBackObj);
            ClientManager.Instance.StatusCallBacks.TryRemove(id,out iCallBackObj);
            ClientManager.Instance.NickNames.Remove(id);
            Queue<Tuple<string, string, Action<string>, string>> shelElement;
            shelElement = TaskQueue.Instance.ShellQueue[id];
            if(shelElement != null)
                foreach(var element in shelElement)
                {
                    //In all removeClient invokes its in AtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, true, 1,true);
                }
            TaskQueue.Instance.ShellQueue.TryRemove(id, out shelElement);
            Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>> transferElement;
            transferElement = TaskQueue.Instance.TransferTaskQueue[id];
            if (transferElement != null)
                foreach (var element in transferElement)
                {
                    //In all removeClient invokes its in AtomicOperation so in order to get into the Deletetask we need to 
                    //pass the lock
                    DeleteClientTask(id, false, 1,true);
                }
            TaskQueue.Instance.TransferTaskQueue.TryRemove(id, out transferElement);
            ClientManager.Instance.Deleted.Add(id);
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
                if (ClientManager.Instance.CallBacks.Count > 0 && id== selectedClient)
                {
                    ClientManager.Instance.SelectedClient = ClientManager.Instance.CallBacks.First().Key;
                }
                else if(ClientManager.Instance.CallBacks.Count == 0 && id == selectedClient)
                {
                    ClientManager.Instance.SelectedClient = null;
                }
            }
        }

        private bool IsClientAlive(string id)
        {
                if (id == null || !ClientManager.Instance.CallBacks.ContainsKey(id))
                {
                    return false;
                }
            try
            {
                var clientCallBack = ClientManager.Instance.StatusCallBacks[id];
                clientCallBack.CallBackFunction("livnessCheck");
                //Must check 2 times, first time go well also if the passiveClient is disconnected
                clientCallBack.CallBackFunction("livnessCheck");

                //Keep the connection with the taks callback alive 
                clientCallBack = ClientManager.Instance.CallBacks[id];
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

        //@pre : ClientManager.Instance.CallBacks.contains(id)
        public void RegisterCallBackFunction(string id, string type)
        {
            if (type == "status")
            {
                
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.StatusCallBacks[id] = callback;
                }
                catch
                {} 
            }
            else
            {
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.CallBacks[id] = callback;
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
                if (ClientManager.Instance.CallBacks.ContainsKey(id))
                    ClientManager.Instance.CallBacks[id].KeepCallbackALive();
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

        private ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>> _shellTaskQueue = new ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>>();
        private ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>> _transferTaskQueue =
            new ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>>();
        public ConcurrentDictionary<string, Queue<Tuple<string, string, Action<string>, string>>> ShellQueue { get { return _shellTaskQueue; } }
        public ConcurrentDictionary<string, Queue<Tuple<string, DownloadRequest, RemoteFileInfo, Action<object>>>> TransferTaskQueue { get { return _transferTaskQueue; } }

        private TaskQueue() { }

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
    private ConcurrentDictionary<string, ICallBack> _statusCallBacks = new ConcurrentDictionary<string, ICallBack>();
    public ConcurrentDictionary<string, ICallBack> StatusCallBacks { get { return _statusCallBacks; } }
    private ConcurrentDictionary<string, ICallBack> _callBacks = new ConcurrentDictionary<string, ICallBack>();
    public ConcurrentDictionary<string, ICallBack> CallBacks { get { return _callBacks; } }
    private Dictionary<string, string> _nickNames = new Dictionary<string, string>();
    public Dictionary<string, string> NickNames { get { return _nickNames; } }
    private HashSet<string> _deleted = new HashSet<string>();
    private HashSet<string> _deletedTasks = new HashSet<string>();
    public HashSet<string> Deleted { get { return _deleted; } }
    public HashSet<string> DeletedTasks { get { return _deletedTasks; } }


    private string _selectedClient;
    public string SelectedClient
        {
            get
            {
               return _selectedClient;
            }
            set
            {
                 _selectedClient = value;
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
        public FileStream FileStream { get; set; }
        public bool IsDownloding { get; set; }
        public bool Buffering { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
        public string FileSize { get; set; }
        private IEnumerator<Tuple<byte[], int>> _enumerableChunk = null;
        public IEnumerator<Tuple<byte[], int>> EnumerableChunk
        {
            get
            {
                if (_enumerableChunk == null)
                {
                    _enumerableChunk = GetNextChunk(Path).GetEnumerator();
                }                
                return _enumerableChunk;
            }
        }
        public int Chunk { get {return 9999999; } }
        public string Path { set; get; }
        public long ReadSoFar { get; set; }
        private static object syncRoot = new Object();
        private static volatile FileMannager instance;
        public bool IsUploading { get; set; }
        public bool UploadingEnded { get; set; }

        private IEnumerable<Tuple<byte[], int>> GetNextChunk(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ReadSoFar = 0;
                while (true)
                {
                    var buffer = new byte[Chunk];
                    var n = fs.Read(buffer, 0, buffer.Length);
                    if (n == 0)
                    {
                        if (_enumerableChunk != null)
                        {
                            _enumerableChunk.Dispose();
                            _enumerableChunk = null;
                        }
                          
                        IsDownloding = false;
                        IsUploading = false;
                        fs.Close();
                        yield break;
                    }
                    ReadSoFar += n;
                    yield return new Tuple<byte[], int>(buffer, n);
                }    
            } 
        }

        private FileMannager() { }
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
