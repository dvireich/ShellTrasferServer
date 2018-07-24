using Data;
using ShellTrasferServer.Data;
using ShellTrasferServer.Helpers;
using ShellTrasferServer.Helpers.interfaces;
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
    public class PassiveShell : IPassiveShell , IActiveShellPassiveshell
    {
        private IPassiveManagerFactory passiveManagerFactory;

        public PassiveShell()
        {
            passiveManagerFactory = new PassiveManagerFactory();
        }

        public PassiveShell(IPassiveManagerFactory passiveManagerFactory)
        {
            this.passiveManagerFactory = passiveManagerFactory;
        }

        public bool PassiveClientRun(string id, string taskId, string baseLine)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveShellHelper.PassiveClientRun(id, taskId, baseLine);
        }

        public bool HasCommand(string id)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveShellHelper.HasShellCommand(id);
        }

        public Tuple<string, string, string> PassiveNextCommand(string id)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveShellHelper.PassiveNextCommand(id);
        }

        public void CommandResponse(string id, string taskId, string baseLine)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveShellHelper.CommandResponse(id, taskId, baseLine);
        }

        public bool HasUploadCommand(string id)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveTransferDataHelper.HasUploadCommand(id);
        }

        public bool HasDownloadCommand(string id)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveTransferDataHelper.HasDownloadCommand(id);
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public DownloadRequest PassiveGetDownloadFile(DownloadRequest req)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveTransferDataHelper.PassiveGetDownloadFile(req);
        }

        [WcfLogging(LogArguments = false)]
        public void PassiveDownloadedFile(RemoteFileInfo request)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveTransferDataHelper.PassiveDownloadedFile(request);
        }

        [WcfLogging(LogArguments = false, LogReturnVal = false)]
        public RemoteFileInfo PassiveGetUploadFile(DownloadRequest req)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveTransferDataHelper.PassiveGetUploadFile(req);
        }

        public void PassiveUploadedFile(string id, string taskId)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveTransferDataHelper.PassiveUploadedFile(id, taskId);
        }

        public void ErrorUploadDownload(string id, string taskId, string response)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveTransferDataHelper.ErrorUploadDownload(id, taskId, response);
        }

        public void ErrorNextCommand(string id, string taskId, string response)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveShellHelper.ErrorNextCommand(id, taskId, response);
        }

        public bool Subscribe(string id, string version, string name)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveClientHelper.Subscribe(id, version, name);
        }

        public void RemoveId(string id)
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveClientHelper.RemoveId(id);
        }

        public bool IsTransferingData()
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            return passiveManager.PassiveTransferDataHelper.IsTransferingData();
        }

        public void StartTransferData()
        {
            var passiveManager = passiveManagerFactory.GetPassiveManager();
            passiveManager.PassiveTransferDataHelper.StartTransferData();
        }
    }
}
