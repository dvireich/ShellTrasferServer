using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAuthentication
{
    class PassiveClientUserConnection : UserConnection
    {
        private string _ip;
        private int _port;

        public PassiveClientUserConnection(string userName, string ip, int port) : base(userName)
        {
            _ip = ip;
            _port = port;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PassiveClientUserConnection other)) return false;
            return other._ip == _ip &&
                   other._port == _port &&
                   base.Equals(other);
        }
    }
}
