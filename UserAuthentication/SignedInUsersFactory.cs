using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserAuthentication.Interfaces;

namespace UserAuthentication
{
    public class SignedInUsersFactory : ISignedInUsersFactory
    {
        public ISignedInUsers GetSignedInUsers()
        {
            return SignedInUsers.Instance;
        }
    }
}
