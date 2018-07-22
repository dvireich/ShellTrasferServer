using Data;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers
{
    public class PassiveClientHelper : IPassiveClientHelper
    {
        private readonly IAtomicOperation atomicOperation;
        private readonly IUserClientManager userClientManager;
        private readonly IUserTaskQueue userTaskQueue;
        private readonly ICommonOperations commonOperations;

        public PassiveClientHelper(IAtomicOperation atomicOperation,
                                   IUserClientManager userClientManager,
                                   IUserTaskQueue userTaskQueue,
                                   ICommonOperations commonOperations)
        {
            this.atomicOperation = atomicOperation;
            this.userClientManager = userClientManager;
            this.userTaskQueue = userTaskQueue;
            this.commonOperations = commonOperations;
        }

        public void RemoveId(string id)
        {
            //unique case that the passive client got some exception and he want to start over, so he delete his
            //previous id and start fresh 

            //This is not part of ActiveClient command process. this was promoted by the passiveClient so there is
            //no conflict in the locks.

            //prevent the active user using the Selected Client or the CallBack Dictionary when in the mean time 
            //is been deleted by the passive client func. Let the first caller finish his operation and then perform
            //the second call
            atomicOperation.PerformAsAtomicFunction(() =>
            {

                commonOperations.RemoveClient(id, true);
            });
            return;
        }

        public bool Subscribe(string id, string version, string name)
        {
            if (version != Constans.Version)
                return false;

            return atomicOperation.PerformAsAtomicSubscribe(() =>
            {
                var currentShellQueue = userTaskQueue.ShellQueue;
                var currentTransferQueue = userTaskQueue.TransferTaskQueue;
                var currentUserCallbacks = userClientManager.CallBacks;


                if (!currentUserCallbacks.ContainsKey(id))
                {
                    currentUserCallbacks[id] = null;
                    userClientManager.NickNames[id] = string.IsNullOrWhiteSpace(name) ? "None" : name;
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

        private void DefineSelectedClient(string id)
        {
            userClientManager.SelectedClient = id;
        }
    }
}
