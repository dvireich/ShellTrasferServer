using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAuthentication
{
    public enum UserType
    {
        ActiveClient,
        PassiveClient
    }
    public class UserConnection
    {
        public string UserName;
        public UserType Type;
        public string Ip;
        public int Port;
    }
}
