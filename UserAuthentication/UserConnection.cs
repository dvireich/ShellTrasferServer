using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAuthentication
{
    public class UserConnection
    {
        private string _userName;

        protected UserConnection(string userName)
        {
            _userName = userName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UserConnection other)) return false;
            return other._userName == _userName;
        }
    }
}
