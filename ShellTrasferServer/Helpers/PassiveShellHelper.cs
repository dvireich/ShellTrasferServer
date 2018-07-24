using Data;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class PassiveShellHelper : IPassiveShellHelper
    {
        private readonly IUserTaskQueue userTaskQueue;
        private readonly IUserClientManager userClientManager;

        public PassiveShellHelper(IUserTaskQueue userTaskQueue,
                                  IUserClientManager userClientManager)
        {
            this.userTaskQueue = userTaskQueue;
            this.userClientManager = userClientManager;
        }

        public bool HasShellCommand(string id)
        {
            var currentUserDeletedTasks = userClientManager.Deleted;
            var currentShellQueue = userTaskQueue.ShellQueue;

            if (currentUserDeletedTasks.Contains(id))
            {
                return true;
            }
            return AllowOnlySelectedClient(id) ?
                  currentShellQueue.ContainsKey(id) && currentShellQueue[id].Count > 0 :
                  false;
        }

        public bool PassiveClientRun(string id, string taskId, string baseLine)
        {
            var currentUserShellQueue = userTaskQueue.ShellQueue;
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

        public Tuple<string, string, string> PassiveNextCommand(string id)
        {
            var currentUserDeletedTasks = userClientManager.Deleted;
            var currentShellQueue = userTaskQueue.ShellQueue;

            if (currentUserDeletedTasks.Contains(id))
            {
                return new Tuple<string, string, string>(Constans.CloseShell, string.Empty, string.Empty);
            }
            //In case that between the has command and the passive client get command the task has been deleted
            if (currentShellQueue[id].Count == 0)
                return new Tuple<string, string, string>(string.Empty, string.Empty, string.Empty);

            var task = currentShellQueue[id].Peek();
            return new Tuple<string, string, string>(task.Command, task.Args, task.TaskId);
        }

        public void CommandResponse(string id, string taskId, string baseLine)
        {
            var currentUserDeletedTasks = userClientManager.Deleted;
            var currentShellQueue = userTaskQueue.ShellQueue;
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

        public void ErrorNextCommand(string id, string taskId, string response)
        {
            var currentShellQueue = userTaskQueue.ShellQueue;
            var currentDeletedTasks = userClientManager.Deleted;

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

        private bool AllowOnlySelectedClient(string id)
        {
            return userClientManager.SelectedClient == id;
        }
    }
}
