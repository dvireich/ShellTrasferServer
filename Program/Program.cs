using DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program
{
    class Program
    {

        public static void CreateDataBase()
        {
            CreateDBHandler createdb = new CreateDBHandler();
            createdb.CreateDataBase();
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
                    }
                }
                return;
            }

            //UserDBManager db = new UserDBManager();
            
        }
    }
}
