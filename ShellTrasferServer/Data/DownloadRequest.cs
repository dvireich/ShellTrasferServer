using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
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
}
