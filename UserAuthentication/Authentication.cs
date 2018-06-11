﻿using DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace UserAuthentication
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Authentication : IAuthentication
    {
        public string Authenticate(string userName, string password)
        {
            var userDBManager = new UserDBManager();
            if (!userDBManager.CheckUserNameAndPassword(userName, password, out User user)) return null;

            return user.Id;
        }

        public bool SignIn(string userName, string password)
        {
            var userDBManager = new UserDBManager();
            if (userDBManager.CheckUserNameAndPassword(userName, password, out User user)) return false;


            userDBManager.SaveUserInDB(new User()
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                Password = password
            });

            return true;
        }
    }
}
