using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace UserAuthentication
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Authentication : IAuthentication
    {
        public string Authenticate(string userName, string password)
        {
            throw new NotImplementedException();
        }
    }
}
