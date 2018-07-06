using Data;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    [Log(AttributeExclude = true)]
    public class UserAtomicOperation
    {
        private static volatile UserAtomicOperation instance;
        private static object syncRoot = new Object();

        private UserAtomicOperation() { }

        public Dictionary<string, AtomicOperation> _userToAtomicOperation = new Dictionary<string, AtomicOperation>();

        public AtomicOperation AtomicOperation
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToAtomicOperation.ContainsKey(activeUserId))
                    _userToAtomicOperation[activeUserId] = new AtomicOperation();
                return _userToAtomicOperation[activeUserId];
            }
        }

        public static UserAtomicOperation Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new UserAtomicOperation();
                    }
                }

                return instance;
            }
        }
    }
}
