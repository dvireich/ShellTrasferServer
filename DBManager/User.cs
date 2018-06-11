using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManager
{
    public class User
    {
        public string Id;
        public string UserName;
        public string Password;

        public string[] ToStringArray()
        {
            return new string[] { Id, UserName, Password };
        }
    }
}
