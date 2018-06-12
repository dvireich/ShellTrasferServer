using DBManager;
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
        public string Authenticate(string userName, string password, out string error)
        {
            error = string.Empty;
            using (var userDBManager = new UserDBManager())
            {
                if (!userDBManager.CheckUserNameAndPassword(userName, password, out User user))
                {
                    error = string.Format("The given user name {0} and password {1} does not exsits in the database", userName, password);
                    return null;
                }
                return user.Id;
            }
        }

        public bool SignIn(string userName, string password, out string error)
        {
            error = string.Empty;
            using (var userDBManager = new UserDBManager())
            {
                if (userDBManager.CheckUserNameExists(userName))
                {
                    error = string.Format("The given user name {0} exsits in the database", userName);
                    return false;
                }

                userDBManager.SaveUserInDB(new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Password = password
                });

                return true;
            }
        }

        public bool ChangeUserPassword(string userName, string oldPassword, string newPassword, out string error)
        {

            using (var userDBManager = new UserDBManager())
            {
                error = string.Empty;
                if (!userDBManager.CheckUserNameAndPassword(userName, oldPassword, out User user))
                {
                    error = string.Format("The given user name {0} and password {1} does not exsits in the database", userName, oldPassword);
                    return false;
                }
                user.Password = newPassword;

                userDBManager.SaveUserInDB(user);

                return true;
            }
        }

        public bool SetSecurityQuestionAndAnswer(string userName, string password, string question, string answer, out string error)
        {
            error = string.Empty;
            using (var userDBManager = new UserDBManager())
            {
                if (!userDBManager.CheckUserNameAndPassword(userName, password, out User user))
                {
                    error = string.Format("The given user name {0} and password {1} does not exsits in the database", userName, password);
                    return false;
                }

                user.SecurityQuestion = question;
                user.SecurityAnswer = answer;

                userDBManager.SaveUserInDB(user);

                return true;
            }  
        }

        public string GetSecurityQuestion(string userName, out string error)
        {
            error = string.Empty;
            using (var userDBManager = new UserDBManager())
            {
                if (!userDBManager.CheckUserNameExists(userName))
                {
                    error = string.Format("The given user name {0} does not exsits in the database", userName);
                    return null;
                }

                var user = userDBManager.GetUserByUserName(userName);
                return user.SecurityQuestion;
            }
        }

        public string RestorePasswordFromUserNameAndSecurityQuestion(string userName, string answer, out string error)
        {
            error = string.Empty;
            using (var userDBManager = new UserDBManager())
            {
                if (!userDBManager.CheckUserNameExists(userName))
                {
                    error = string.Format("The given user name {0} does not exsits in the database", userName);
                    return null;
                }

                var user = userDBManager.GetUserByUserName(userName);
                if (user.SecurityAnswer == answer)
                {
                    return user.Password;
                }
                else
                {
                    error = string.Format("Answer provided to {0} is not match!", user.SecurityQuestion);
                    return null;
                }
            }
        }
    }
}
