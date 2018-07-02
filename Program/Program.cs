﻿using DBManager;
using UserLoader;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

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
            try
            {
                while (true)
                {
                    if (!TaskQueue.Instance.Any()) continue;

                    var id = TaskQueue.Instance.GetNextTask();
                    ServiceLoader.LoadShellTransferServices(id);
                }
            }
            finally
            {
                ServiceLoader.CloseAllChnnels();
            }
        }
        

        //[Log(AttributeExclude = true)]
        static void Main(string[] args)
        {
            //InitializeLoggingBackend();
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

        //[Log(AttributeExclude = true)]
        //public static void InitializeLoggingBackend()
        //{
        //    log4net.Config.XmlConfigurator.Configure();
        //    var log4NetLoggingBackend = new Log4NetLoggingBackend();
        //    LoggingServices.DefaultBackend = log4NetLoggingBackend;
        //}

    }
}
