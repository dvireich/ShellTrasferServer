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
    public class FileMannager
    {
        private static object syncRoot = new Object();
        private static volatile FileMannager instance;
        private FileMannager() { }


        private ConcurrentDictionary<string, UserFileManager> _userToUserFileMannager = new ConcurrentDictionary<string, UserFileManager>();
        public UserFileManager CurrentUserFileMannager
        {
            get
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress.ToString();
                var activeUserId = endpoint.Split('/').Last();
                if (!_userToUserFileMannager.ContainsKey(activeUserId))
                    _userToUserFileMannager[activeUserId] = new UserFileManager();
                return _userToUserFileMannager[activeUserId];
            }
        }

        public static FileMannager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FileMannager();
                    }
                }

                return instance;
            }
        }

    }
}
