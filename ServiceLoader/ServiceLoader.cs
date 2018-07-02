using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using UserAuthentication;
using UserLoader;
using ShellTrasferServer;
using ServiceLoader;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Program
{
    public static class ServiceLoader
    {
        private static List<ServiceHost> OpenChnnels = new List<ServiceHost>();
        private static HashSet<string> OpenIds = new HashSet<string>();

        public static void LoadBasicServices()
        {
            InitializeBasicHttpServiceReferences<Authentication,IAuthentication>("Authentication");
            InitializeBasicHttpServiceReferences<LoadUser, ILoadUser>("LoadUser");
        }

        public static void LoadShellTransferServices(string id)
        {
            if (OpenIds.Contains(id)) return;
            InitializeBasicHttpServiceReferences<ShellTransfer, IActiveShell>(string.Format("ActiveShell/{0}", id));
            InitializeBasicHttpServiceReferences<ShellTransfer, IPassiveShell>(string.Format("PassiveShell/{0}", id));
            InitializeTCPServiceReferences<ShellTransfer, IAletCallBack>(string.Format("CallBack/{0}", id));
        }

        private static void InitializeBasicHttpServiceReferences<TC,TI>(string id)
        {
            //Confuguring the Shell service
            var shellBinding = new BasicHttpBinding();
            shellBinding.Security.Mode = BasicHttpSecurityMode.None;
            shellBinding.CloseTimeout = TimeSpan.MaxValue;
            shellBinding.ReceiveTimeout = TimeSpan.MaxValue;
            shellBinding.SendTimeout = new TimeSpan(0, 0, 10, 0, 0);
            shellBinding.OpenTimeout = TimeSpan.MaxValue;
            shellBinding.MaxReceivedMessageSize = int.MaxValue;
            shellBinding.MaxBufferPoolSize = int.MaxValue;
            shellBinding.MaxBufferSize = int.MaxValue;
            //Put Public ip of the server copmuter
            var shellAdress = string.Format("http://localhost:80/ShellTrasferServer/{0}",id);
            var shellUri = new Uri(shellAdress);

            var serviceHost = new ServiceHost(typeof(TC), shellUri);
            var smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            serviceHost.Description.Behaviors.Add(smb);

            serviceHost.AddServiceEndpoint(typeof(TI), shellBinding, shellAdress);
            serviceHost.Open();
            OpenIds.Add(id.Split('/').Last());
            OpenChnnels.Add(serviceHost);
        }

        public static void InitializeTCPServiceReferences<TC,TI>(string path)
        {
            Uri endPointAdress = new Uri(string.Format("net.tcp://localhost/ShellTrasferServer/{0}",path));
            NetTcpBinding wsd = new NetTcpBinding();
            wsd.Security.Mode = SecurityMode.None;
            wsd.CloseTimeout = TimeSpan.MaxValue;
            wsd.ReceiveTimeout = TimeSpan.MaxValue;
            wsd.OpenTimeout = TimeSpan.MaxValue;
            wsd.SendTimeout = TimeSpan.MaxValue;
            EndpointAddress ea = new EndpointAddress(endPointAdress);


            var serviceHost = new ServiceHost(typeof(TC), endPointAdress);
            var smb = new ServiceMetadataBehavior();
            serviceHost.Description.Behaviors.Add(smb);

            serviceHost.AddServiceEndpoint(typeof(TI), wsd, endPointAdress);
            serviceHost.Open();
        }

        public static void CloseAllChnnels()
        {
            foreach(var channel in OpenChnnels)
            {
                channel.Close();
            }
        }
    }
}
