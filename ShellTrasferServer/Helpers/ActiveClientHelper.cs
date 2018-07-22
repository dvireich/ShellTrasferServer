using Data;
using ShellTrasferServer.Data;
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
    public class ActiveClientHelper : IActiveClientHelper
    {
        private readonly IAtomicOperation atomicOperation;
        private readonly IUserClientManager userClientManager;
        private readonly IUserTaskQueue userTaskQueue;
        private readonly ICommonOperations commonOperations;

        public ActiveClientHelper(IAtomicOperation atomicOperation,
                                  IUserClientManager userClientManager,
                                  IUserTaskQueue userTaskQueue,
                                  ICommonOperations commonOperations)
        {
            this.atomicOperation = atomicOperation;
            this.userClientManager = userClientManager;
            this.userTaskQueue = userTaskQueue;
            this.commonOperations = commonOperations;
        }

        public bool ActiveSetNickName(string id, string nickName)
        {
            var temp = userClientManager.NickNames[id];
            try
            {
                var curretUserCallbacks = userClientManager.CallBacks[id];
                userClientManager.NickNames[id] = nickName;
                curretUserCallbacks.CallBackFunction(string.Format("nickName : {0}", nickName));
                return true;
            }
            catch
            {
                userClientManager.NickNames[id] = temp;
                return false;
            }
        }

        public bool ClearAllData(string id)
        {
            return atomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                userClientManager.Deleted.Clear();
                userClientManager.SelectedClient = null;
                foreach (var client in userClientManager.CallBacks.Keys)
                {
                    commonOperations.RemoveClient(client);
                }
                userClientManager.NickNames.Clear();
                userClientManager.CallBacks.Clear();
                userClientManager.StatusCallBacks.Clear();
                userTaskQueue.ShellQueue.Clear();
                userTaskQueue.TransferTaskQueue.Clear();
                return true;
            });
        }

        public bool SelectClient(string id)
        {
            return atomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                var clients = userClientManager.CallBacks;
                if (!clients.ContainsKey(id))
                    return false;

                if (!IsClientAlive(id))
                {
                    commonOperations.RemoveClient(id);
                    return false;
                }
                //connect the slected client
                userClientManager.SelectedClient = id;
                return true;
            });
        }

        public bool ActiveCloseClient(string id)
        {
            return atomicOperation.PerformAsAtomicFunction<bool>(() =>
            {
                if (userClientManager.CallBacks.ContainsKey(id))
                {
                    commonOperations.RemoveClient(id);
                    return true;
                }
                return false;
            });
        }

        public string GetStatus()
        {
            return atomicOperation.PerformAsAtomicFunction((() =>
            {
                var clients = userClientManager.CallBacks.ToList();
                var status = new StringBuilder();
                var clientCounter = 1;
                status.AppendLine("The Status: ");
                if (clients.Count > 0)
                {
                    foreach (var client in clients)
                    {
                        var isAlive = IsClientAlive(client.Key);
                        var nickName = userClientManager.NickNames[client.Key];
                        status.AppendLine(string.Format("Client number{0} id: {1}\t" +
                                                        "NickName:{3}\n" +
                                                        "Is Alive: {2}"
                                                          , clientCounter, client.Key, isAlive, nickName));
                        var taskCounter = 1;

                        ParseShellTasks(status, client.Key, ref taskCounter);
                        ParseUploadDownloadTasks(status, client, taskCounter);

                        status.AppendLine(string.Format(""));

                        clientCounter++;
                    }
                    status.AppendLine(string.Format("The selected Client id is: {0}",
                        userClientManager.SelectedClient));
                }
                else
                {
                    status.AppendLine(string.Format("There is no clients connected"));
                }
                return status.ToString();
            }));
        }

        private void ParseShellTasks(StringBuilder status, string client, ref int taskCounter)
        {
            if (userTaskQueue.ShellQueue.ContainsKey(client))
            {
                status.AppendLine("Shell Tasks:");
                var clientTasks = userTaskQueue.ShellQueue[client].ToList();
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
        }

        private int ParseUploadDownloadTasks(StringBuilder status, KeyValuePair<string, ICallBack> client, int taskCounter)
        {
            if (userTaskQueue.TransferTaskQueue.ContainsKey(client.Key))
            {
                taskCounter = 1;
                status.AppendLine("Upload And Download Tasks:");
                var clientTasks = userTaskQueue.TransferTaskQueue[client.Key].ToList();
                if (clientTasks.Count > 0)
                {
                    foreach (var task in clientTasks)
                    {
                        status.AppendLine(string.Format("Task Number: {0}", taskCounter));

                        status.AppendLine(task.ToString());

                        taskCounter++;
                    }
                }
                else
                {
                    status.AppendLine("There is no Download or Upload tasks");
                }
            }

            return taskCounter;
        }

        private bool IsClientAlive(string id)
        {
            var currentUserCallbacks = userClientManager.CallBacks;
            var currentUserStatusCallbacks = userClientManager.StatusCallBacks;

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

        public bool DeleteClientTask(string id, bool shellTask, int taksNumber)
        {
            return commonOperations.DeleteClientTask(id, shellTask, taksNumber);
        }
    }
}
