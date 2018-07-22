using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer.Helpers.interfaces
{
    public interface IFileHelper
    {
        bool Exists(string path);

        Stream Create(string path);

        void Delete(string path);

        FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share);
    }
}
