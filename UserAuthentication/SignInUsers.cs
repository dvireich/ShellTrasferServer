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

        public bool AddToList(string username, string type)
        {
            try
            {
                List<UserConnection> userConnections;
                if (!usersLoggedIn.ContainsKey(username))
                {
                    usersLoggedIn.TryAdd(username, new List<UserConnection>());
                }

                if (!usersLoggedIn.TryGetValue(username, out userConnections)) return false;

                OperationContext oOperationContext = OperationContext.Current;
                MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
                RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];

                string szAddress = oRemoteEndpointMessageProperty.Address;
                int nPort = oRemoteEndpointMessageProperty.Port;

                var userConnection = new UserConnection()
                {
                    Type = (UserType)Enum.Parse(typeof(UserType), type),
                    UserName = username,
                    Ip = szAddress,
                    Port = nPort
                };

                lock (syncUsersListLoggedIn)
                {
                    userConnections.Add(userConnection);
                }

                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool RemoveFromList(string username, string type)
        {
            List<UserConnection> userConnections;
            if (!usersLoggedIn.TryGetValue(username, out userConnections)) return false;

            var enumType = (UserType)Enum.Parse(typeof(UserType), type);
            OperationContext oOperationContext = OperationContext.Current;
            MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
            RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];
            string szAddress = oRemoteEndpointMessageProperty.Address;
            int nPort = oRemoteEndpointMessageProperty.Port;
            lock (syncUsersListLoggedIn)
            {
                userConnections.RemoveAll(user => user.Type == enumType &&
                                          user.UserName == username &&
                                          user.Ip == szAddress &&
                                          user.Port == nPort);
            }

            return true;
        }

        public bool ExsitsInList(string username, string type)
        {
            List<UserConnection> userConnections;
            if (!usersLoggedIn.TryGetValue(username, out userConnections)) return false;

            var enumType = (UserType)Enum.Parse(typeof(UserType), type);
            OperationContext oOperationContext = OperationContext.Current;
            MessageProperties oMessageProperties = oOperationContext.IncomingMessageProperties;
            RemoteEndpointMessageProperty oRemoteEndpointMessageProperty = (RemoteEndpointMessageProperty)oMessageProperties[RemoteEndpointMessageProperty.Name];
            string szAddress = oRemoteEndpointMessageProperty.Address;
            int nPort = oRemoteEndpointMessageProperty.Port;

            //We indentify PassiveClient with ip and port.
            //We indentify Activeclient only with ip sience we not allow more that 1 ActiveClient
            lock (syncUsersListLoggedIn)
            {
                if (enumType == UserType.PassiveClient)
                {
                    return userConnections.Any(user => user.Type == enumType &&
                                         user.UserName == username &&
                                         user.Ip == szAddress &&
                                         user.Port == nPort);
                }

                return userConnections.Any(user => user.Type == enumType &&
                                         user.UserName == username &&
                                         user.Ip == szAddress);

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
