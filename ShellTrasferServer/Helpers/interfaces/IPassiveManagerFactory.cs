using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IPassiveManagerFactory
    {
        IPassiveManager GetPassiveManager();
    }
}
