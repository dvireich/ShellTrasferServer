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
        string AuthenticatePassiveClientAndSignIn(string userName, string password, out string error);

        [OperationContract]
        string AuthenticateActiveClientAndSignIn(string userName, string password, out string error);

        [OperationContract]
        bool ActiveLogout(string userName, string userType, out string error);

        [OperationContract]
        bool PassiveLogout(string userName, string userType, out string error);

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
