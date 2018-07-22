﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManager.Interfaces
{
    public interface IUserRepository : IDisposable
    {
        bool CheckUserNameExists(string userName);

        bool CheckUserExists(User user);

        bool CheckUserNameAndPassword(string userName, string password, out User user);

        void SaveUserInDB(User user);

        void DeleteUserFromDB(User user);

        User GetUserByUserName(string userName);
    }
}
