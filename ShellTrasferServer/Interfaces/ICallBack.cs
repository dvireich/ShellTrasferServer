using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    public interface ICallBack
    {
        [OperationContract(IsOneWay = true)]
        void CallBackFunction(string str);

        [OperationContract(IsOneWay = true)]
        void KeepCallbackALive();
    }
}
