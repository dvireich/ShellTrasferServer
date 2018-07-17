using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Data;
using System.IO;
using ShellTrasferServer;
using System.Threading;
using System.Net.Http;
using System.Collections.Concurrent;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;
using WcfLogger;
using PostSharp.Patterns.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace ShellTrasferServer
{
    [WcfLogging]
    [Log(AttributeExclude = true)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class AletCallBack : IAletCallBack
    {

        #region ICallBack Implementation

        //@pre : ClientManager.Instance.UserToUserClientManager.CallBacks.contains(id)
        public void RegisterCallBackFunction(string id, string type)
        {
            if (type == "status")
            {
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.CurretUserClientManager.StatusCallBacks[id] = callback;
                }
                catch
                {} 
            }
            else
            {
                try
                {
                    ICallBack callback = OperationContext.Current.GetCallbackChannel<ICallBack>();
                    ClientManager.Instance.CurretUserClientManager.CallBacks[id] = callback;
                }
                catch
                {}
            }
        }

        public void KeepCallBackAlive(string id)
        {
            try
            {
                //Send data through the connection pipe
                if (ClientManager.Instance.CurretUserClientManager.CallBacks.ContainsKey(id))
                    ClientManager.Instance.CurretUserClientManager.CallBacks[id].KeepCallbackALive();
            }
            catch {} 
        }
        #endregion ICallBack Implementation
    }
}
