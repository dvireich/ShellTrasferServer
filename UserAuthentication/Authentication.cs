using DBManager;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using WcfLogger;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace UserAuthentication
{
    [WcfLogging]
    public class Authentication : IAuthentication
    {
        public string AuthenticateAndSignIn(string userName, string userType, string password, out string error)
        {
            
            if(SignInUsers.Instance.ExsitsInList(userName, userType))
            {
                error = $"The user name: {userName} is already signed in!, please logout first";
                return null;
            }


            using (var userDBManager = new UserDBManager())
            {
                if (!userDBManager.CheckUserNameAndPassword(userName, password, out User user))
                {
                    error = string.Format("The given user name {0} and password {1} does not exsits in the database", userName, password);
                    return null;
                }

                if(!SignInUsers.Instance.AddToList(userName , userType))
                {
                    error = string.Format("Error in sign in with user name {0} and user type {1}. Check that the user type is correct", userName, userType);
                    return null;
                }

                error = string.Empty;
                return user.Id;
            }
        }

        public bool SignUp(string userName, string password, out string error)
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

        public bool Logout(string userName, string userType , out string error)
        {
            error = string.Empty;
            try
            {
                return SignInUsers.Instance.RemoveFromList(userName, userType);
            }
            catch(Exception e)
            {
                error = $"Error in Logout for: {userName} with the following error: {e.Message}";
                return false;
            }
        }
    }
}
