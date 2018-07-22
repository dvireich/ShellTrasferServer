using Data;
using ShellTrasferServer.Data;
using ShellTrasferServer.Helpers;
using ShellTrasferServer.Helpers.interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfLogger;

namespace ShellTrasferServer
{
    [WcfLogging]
    public class ActiveShell : ActiveShellPassiveshell , IActiveShell
    {
        IActiveManagerFactory activeManagerFactory;
        public ActiveShell()
        {
            activeManagerFactory = new ActiveManagerFactory();
        }

        public bool ActiveSetNickName(string id, string nickName)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.ActiveSetNickName(id, nickName);
        }

        public bool DeleteClientTask(string id, bool shellTask, int taksNumber)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.DeleteClientTask(id, shellTask, taksNumber);
        }

        public bool ClearAllData(string id)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.ClearAllData(id);
        }

        public bool ActiveCloseClient(string id)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.ActiveCloseClient(id);
        }

        public bool SelectClient(string id)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.SelectClient(id);
        }

        public string GetStatus()
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ClientHelper.GetStatus();
        }

        public void ClearQueue()
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            activeManager.ShellHelper.ClearQueue();
        }

        public string ActiveNextCommand(string args)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ShellHelper.ActiveNextCommand(args);
        }

        public string ActiveClientRun()
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.ShellHelper.ActiveClientRun();
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public RemoteFileInfo ActiveDownloadFile(DownloadRequest request)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.TransferDataHelper.ActiveDownloadFile(request);
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public RemoteFileInfo ActiveUploadFile(RemoteFileInfo request)
        {
            var activeManager = activeManagerFactory.GetActiveManager();
            return activeManager.TransferDataHelper.ActiveUploadFile(request);
        }
    }
}

   
