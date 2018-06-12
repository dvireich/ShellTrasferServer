using DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserLoader;

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
        }

        static void Main(string[] args)
        {
            //SqlManager a = new SqlManager();
            //a.Connect("");
            //a.CreateDataBase();
            //a.InsertIntoDataBase("Users", new string[] { "a", "b", "c" });
            ////a.UpdateDataBaseRecord("Users", "a", new string[] { "Id", "User_Name", "Password" }, new string[] { "b", "b", "b" });
            //var ans = a.GetRecord("Users", "b");
            //a.DeleteRecord("Users", "b");
            //a.Disconnect();

            if(args.Length > 0)
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
    }
}
