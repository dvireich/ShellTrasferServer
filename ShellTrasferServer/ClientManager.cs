using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class ClientManager
    {

        private static volatile ClientManager instance;
        private static object syncRoot = new Object();
        private ClientManager() { }

        private ConcurrentDictionary<string, UserClientManager> _userToUserClientManager = new ConcurrentDictionary<string, UserClientManager>();
        public UserClientManager CurretUserClientManager
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserClientManager.ContainsKey(activeUserId))
                    _userToUserClientManager[activeUserId] = new UserClientManager();
                return _userToUserClientManager[activeUserId];
            }
        }

        public static ClientManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new ClientManager();
                    }
                }

                return instance;
            }
        }

    }
}
