using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

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
}
