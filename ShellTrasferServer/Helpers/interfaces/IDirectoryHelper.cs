using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IDirectoryHelper
    {
        string GetTempPath();

        bool Exists(string path);

        void CreateDirectory(string path);
    }
}
