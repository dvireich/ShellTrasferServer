using PostSharp.Extensibility;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using UserAuthentication.Interfaces;

namespace UserAuthentication
{
    [Log(AttributeTargetElements = MulticastTargets.Method, AttributeTargetTypeAttributes = MulticastAttributes.Public, AttributeTargetMemberAttributes = MulticastAttributes.Public)]
    public class SignedInUsers : ISignedInUsers
    {
        private static volatile SignedInUsers instance;
        private static object syncRoot = new Object();

        private ConcurrentDictionary<string, List<UserConnection>> usersLoggedIn = new ConcurrentDictionary<string, List<UserConnection>>();
        private static object syncUsersListLoggedIn = new Object();

        public bool AddPassiveClient(string username)
        {
            if (!AddIfNotExistsAndValidate(username, out var userConnections)) return false;

            OperationContext oOperationContext = OperationContext.Current;
            MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
            RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];

            string szAddress = oRemoteEndpointMessageProperty.Address;
            int nPort = oRemoteEndpointMessageProperty.Port;

            var userConnection = new PassiveClientUserConnection(username, szAddress, nPort);

            lock (syncUsersListLoggedIn)
            {
                userConnections.Add(userConnection);
            }

            return true;

        }

        public bool AddActiveClient(string username)
        {
            if (!AddIfNotExistsAndValidate(username, out var userConnections)) return false;

            var userConnection = new ActiveClientUserConnection(username);

            lock (syncUsersListLoggedIn)
            {
                userConnections.Add(userConnection);
            }

            return true;
        }

        private bool AddIfNotExistsAndValidate(string username, out List<UserConnection> Connections)
        {
            Connections = null;
            if (!usersLoggedIn.ContainsKey(username))
            {
                usersLoggedIn.TryAdd(username, new List<UserConnection>());
            }

            if (!usersLoggedIn.TryGetValue(username, out List<UserConnection> userConnections)) return false;
            Connections = userConnections;
            return true;
        }

        public bool RemoveActiveClientFromList(string userName)
        {
            if (!usersLoggedIn.TryGetValue(userName, out List<UserConnection> userConnections)) return false;

            var activeClientToCompare = new ActiveClientUserConnection(userName);
            lock (syncUsersListLoggedIn)
            {
                userConnections.RemoveAll(user => user.Equals(activeClientToCompare));
            }

            return true;

        }

        public bool RemovePassiveClientFormList(string userName)
        {
            if (!usersLoggedIn.TryGetValue(userName, out List<UserConnection> userConnections)) return false;

            OperationContext oOperationContext = OperationContext.Current;
            MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
            RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];
            string szAddress = oRemoteEndpointMessageProperty.Address;
            int nPort = oRemoteEndpointMessageProperty.Port;

            var passiveClientToCompare = new PassiveClientUserConnection(userName, szAddress, nPort);

            lock (syncUsersListLoggedIn)
            {
                userConnections.RemoveAll(user => user.Equals(passiveClientToCompare));
            }

            return true;
        }

        public bool ActiveClientExsitsInList(string userName)
        {
            if (!usersLoggedIn.TryGetValue(userName, out List<UserConnection> userConnections)) return false;

            var activeClientToCompare = new ActiveClientUserConnection(userName);

            lock (syncUsersListLoggedIn)
            {
                return userConnections.Any(user => user.Equals(activeClientToCompare));
            }
        }

        [Log(AttributeExclude = true)]
        private SignedInUsers() { }

        [Log(AttributeExclude = true)]
        public static SignedInUsers Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new SignedInUsers();
                    }
                }

                return instance;
            }
        }
    }
}
