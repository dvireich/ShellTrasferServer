using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
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
