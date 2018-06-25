using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Net.Http;


namespace ShellTrasferServer
{

    [ServiceContract]
    public interface IRestService
    {
        [OperationContract]
        [WebGet(UriTemplate = "Status", BodyStyle = WebMessageBodyStyle.Bare)]
        System.IO.Stream RestGetStatus();

        //All this method down low should be Post methods, but in order to execute them from the browser url they been written
        //as get methods

        //[OperationContract]
        //[WebGet(UriTemplate = "SelectClient {id}", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream RestSelectClient(string id);

        //[OperationContract]
        //[WebGet(UriTemplate = "ClearAllData", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream ClearAllData();

        //[OperationContract]
        //[WebGet(UriTemplate = "CloseClient {id}", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream CloseClient(string id);

        //[OperationContract]
        //[WebGet(UriTemplate = "DeleteClientTask {ClientId} {type} {number}", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream DeleteClientTask(string ClientId, string type, string number);

        //[OperationContract]
        //[WebGet(UriTemplate = "SetNickName {ClientId} {nickName}", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream SetNickName(string ClientId, string nickName);

        //[OperationContract]
        //[WebGet(UriTemplate = "Run", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream Run();

        //[OperationContract]
        //[WebGet(UriTemplate = "NextCommand {command}", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream NextCommand(string command);

        //[OperationContract]
        //[WebGet(UriTemplate = "", BodyStyle = WebMessageBodyStyle.Bare)]
        //System.IO.Stream Help();

    }

    [ServiceContract(CallbackContract = typeof(ICallBack))]
    public interface IAletCallBack  
    {
        [OperationContract(IsOneWay = true)]
        void RegisterCallBackFunction(string id,string type);

        [OperationContract(IsOneWay = true)]
        void KeepCallBackAlive(string id);
    }

    public interface ICallBack
    {
        [OperationContract(IsOneWay = true)]
        void CallBackFunction(string str);

        [OperationContract(IsOneWay = true)]
        void KeepCallbackALive();
    }

    [ServiceContract]
    public interface IActiveShellPassiveshell
    {
        [OperationContract]
        bool IsTransferingData();

        [OperationContract]
        void StartTransferData();
    }



        [ServiceContract]
    public interface IActiveShell : IActiveShellPassiveshell
    {
        [OperationContract]
        string ActiveNextCommand(string command);       
        [OperationContract]
        string ActiveClientRun();
        [OperationContract]
        RemoteFileInfo ActiveDownloadFile(DownloadRequest request);
        [OperationContract]
        RemoteFileInfo ActiveUploadFile(RemoteFileInfo request);
        [OperationContract]
        void ClearQueue();
        [OperationContract]
        string GetStatus();
        [OperationContract]
        bool SelectClient(string id);
        [OperationContract]
        bool ActiveCloseClient(string id);
        [OperationContract]
        bool ClearAllData(string id);
        [OperationContract]
        bool DeleteClientTask(string id,bool shellTask,int taksNumber);
        [OperationContract]
        bool ActiveSetNickName(string id, string nickName);
    }

    [ServiceContract]
    public interface IPassiveShell : IActiveShellPassiveshell
    {  
        [OperationContract]
        bool PassiveClientRun(string id, string taskId, string baseLine);
        [OperationContract]
        Tuple<string, string, string> PassiveNextCommand(string id);
        [OperationContract]
        void CommandResponse(string id, string taskId, string baseLine);
        [OperationContract]
        bool HasCommand(string id);
        [OperationContract]
        bool HasUploadCommand(string id);
        [OperationContract]
        bool HasDownloadCommand(string id);
        [OperationContract]
        void PassiveDownloadedFile(RemoteFileInfo request);
        [OperationContract]
        DownloadRequest PassiveGetDownloadFile(DownloadRequest id);
        [OperationContract]
        RemoteFileInfo PassiveGetUploadFile(DownloadRequest id);
        [OperationContract]
        void PassiveUploadedFile(string id, string taskId);
        [OperationContract]
        void ErrorUploadDownload(string id, string taskId, string response);
        [OperationContract]
        bool Subscribe(string id, string version, string nickName);
        [OperationContract]
        void ErrorNextCommand(string id, string taskId, string response);
    }

    [MessageContract]
    public class DownloadRequest
    {
        [MessageBodyMember]
        public string FileName;

        [MessageBodyMember]
        public string id;

        [MessageBodyMember]
        public string taskId;

        [MessageBodyMember]
        public string PathInServer;

        [MessageBodyMember]
        public bool NewStart;

        [MessageBodyMember]
        public string PathToSaveInClient;
    }

    [MessageContract]
    public class RemoteFileInfo : IDisposable
    {
        [MessageHeader(MustUnderstand = true)]
        public string FileName;

        [MessageHeader(MustUnderstand = true)]
        public string PathToSaveOnServer;

        [MessageHeader(MustUnderstand = true)]
        public string FileSize;

        [MessageHeader(MustUnderstand = true)]
        public long Length;

        [MessageHeader(MustUnderstand = true)]
        public bool FileEnded;

        [MessageHeader(MustUnderstand = true)]
        public bool FreshStart;

        [MessageHeader(MustUnderstand = true)]
        public string id;

        [MessageHeader(MustUnderstand = true)]
        public string taskId;

        [MessageBodyMember(Order = 1)]
        public byte[] FileByteStream;

        public void Dispose()
        {
            FileByteStream = null;
        }
    }
}
