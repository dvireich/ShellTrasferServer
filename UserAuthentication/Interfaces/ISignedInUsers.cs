using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAuthentication.Interfaces
{
    public interface ISignedInUsers
    {
        bool AddToList(string username, string type);

        bool RemoveFromList(string username, string type);

        bool ExsitsInList(string username, string type);
    }
}
