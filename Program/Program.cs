using DBManager;
using PostSharp.Patterns.Diagnostics;
using PostSharp.Patterns.Diagnostics.Backends.Console;
using System;
using System.Diagnostics;
using System.IO;
using UserLoader;

[assembly: Log]


namespace Program
{
   
    public static class Program
    {
        
        private static void CreateDataBase()
        {
            using (var createdb = new CreateDBHandler())
            {
                createdb.CreateDataBase();
            }
        }

        private static void RunAsConsole()
        {
            ServiceLoader.LoadBasicServices();


            while (true)
            {
                if (!TaskQueue.Instance.Any()) continue;

                var id = TaskQueue.Instance.GetNextTask();
                ServiceLoader.LoadShellTransferServices(id);
            }

            //ServiceLoader.CloseAllChnnels();
        }

        [Log(AttributeExclude = true)]
        static void Main(string[] args)
        {
            InitializeLoggingBackend();
            if (args.Length > 0)
            {
                foreach(var arg in args)
                {
                    if (arg.ToLower() == @"/CreateDB".ToLower())
                    {
                        CreateDataBase();
                        return;
                    }
                    if (arg.ToLower() == @"/Console".ToLower())
                    {
                        RunAsConsole();
                        return;
                    }

                }
            }
        }

        [Log(AttributeExclude = true)]
        public static void InitializeLoggingBackend()
        {
            var consoleLogging = new ConsoleLoggingBackend();
            consoleLogging.Options.TimestampFormat = "MM/dd/yyyy hh:mm:ss.ffff tt";
            consoleLogging.Options.IncludeTimestamp = true;
            LoggingServices.DefaultBackend = consoleLogging;
            
            //FileStream fs = new FileStream("Log.txt", FileMode.OpenOrCreate);
            //StreamWriter sw = new StreamWriter(fs);
            //Console.SetOut(sw);
        }

    }
}
