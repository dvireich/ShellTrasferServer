using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserAuthentication.Interfaces
{
    public interface ISignedInUsers
    {
        bool AddPassiveClient(string username);

        bool AddActiveClient(string username);

        bool RemoveActiveClientFromList(string userName);

        bool RemovePassiveClientFormList(string userName);

        bool ActiveClientExsitsInList(string userName);
    }
}
