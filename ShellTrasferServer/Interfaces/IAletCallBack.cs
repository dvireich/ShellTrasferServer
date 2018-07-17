using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    [ServiceContract(CallbackContract = typeof(ICallBack))]
    public interface IAletCallBack
    {
        [OperationContract(IsOneWay = true)]
        void RegisterCallBackFunction(string id, string type);

        [OperationContract(IsOneWay = true)]
        void KeepCallBackAlive(string id);
    }
}
