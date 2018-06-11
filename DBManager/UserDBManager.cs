using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManager
{
     public class UserDBManager
    {
        SqlManager sql;

        readonly string[] userCol = new string[]
        {
            Schema.Columns.Id.ToString(),
            Schema.Columns.User_Name.ToString(),
            Schema.Columns.Password.ToString(),
        };

        public UserDBManager()
        {
            sql = new SqlManager();
            sql.Connect();
        }

        public void Exit()
        {
            if(sql != null)
                sql.Disconnect();
        }

        public bool CheckUserNameExists(string userName)
        {
            var resp = sql.GetRecord(Schema.Tables.Users.ToString(), Schema.Columns.User_Name.ToString(), userName);
            return resp != null && resp.Length > 0;
        }

        public bool CheckUserExists(User user)
        {
            var resp = sql.GetRecord(Schema.Tables.Users.ToString(), Schema.Columns.Id.ToString(), user.Id);
            return resp != null && resp.Length > 0;
        }

        public bool CheckUserNameAndPassword(string userName, string password, out User user)
        {
            user = null;
            if (!CheckUserNameExists(userName)) return false;
            var userList = new List<User>();
            var resp = sql.GetRecord(Schema.Tables.Users.ToString(), Schema.Columns.User_Name.ToString(), userName);
            for(int i = 0; i < resp.Length; i = i + 3)
            {
                userList.Add(new User()
                {
                    Id = resp[i].Trim(),
                    UserName = resp[i + 1].Trim(),
                    Password = resp[i + 2].Trim()
                });
            }

            user = userList.FirstOrDefault(u => u.Password == password);

            return user != null;
        }

        public void SaveUserInDB(User user)
        {
            if(CheckUserExists(user))
            {
                sql.UpdateDataBaseRecord(Schema.Tables.Users.ToString(), user.Id, userCol, user.ToStringArray());
            }
            else
            {
                sql.InsertIntoDataBase(Schema.Tables.Users.ToString(), user.ToStringArray());
            }
        }

        public void DeleteUserFromDB(User user)
        {
            if (!CheckUserExists(user)) return;

            sql.DeleteRecord(Schema.Tables.Users.ToString(), user.Id);
        }
    }
}
