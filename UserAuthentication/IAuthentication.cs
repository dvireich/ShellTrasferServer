using DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace UserAuthentication
{
    [ServiceContract]
    public interface IAuthentication
    {
        [OperationContract]
        string AuthenticateAndSignIn(string userName, string userType , string password, out string error);

        [OperationContract]
        bool Logout(string userName, string userType, out string error);

        [OperationContract]
        bool SignUp(string userName, string password, out string error);

        [OperationContract]
        bool ChangeUserPassword(string userName, string oldPassword, string newPassword, out string error);

        [OperationContract]
        bool SetSecurityQuestionAndAnswer(string userName, string password, string question, string answer, out string error);

        [OperationContract]
        string GetSecurityQuestion(string userName, out string error);

        [OperationContract]
        string RestorePasswordFromUserNameAndSecurityQuestion(string userName, string answer, out string error);
    }


}
