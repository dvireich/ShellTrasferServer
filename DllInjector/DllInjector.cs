using MethodLogger;
using PERWAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DllInjector
{
    public static class DllInjector
    {
        public static void StartInjection()
        {
            //MethodLoggerUtil.ProcessFile("DBManager.dll");
            //MethodLoggerUtil.ProcessFile(@"ServiceLoader.dll");
            //MethodLoggerUtil.ProcessFile("ShellTrasferServer.dll");
            MethodLoggerUtil.ProcessFile("UserAuthentication.dll");
            //MethodLoggerUtil.ProcessFile(@"UserLoader\UserLoader.dll");
            //MethodLoggerUtil.ProcessFile(@"Program.exe");
        }

        public static void Main(string[] args)
        {
            StartInjection();
        }
    }
}
